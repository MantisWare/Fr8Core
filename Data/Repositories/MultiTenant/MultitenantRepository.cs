﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Data.Crates;
using Data.Interfaces;
using Data.Interfaces.Manifests;
using Data.Repositories.MultiTenant.Ast;
using Data.Repositories.MultiTenant.Queryable;

namespace Data.Repositories.MultiTenant
{
    partial class MultitenantRepository : IMultiTenantObjectRepository
    {
        private readonly IMtTypeStorageProvider _typeStorageProvider;
        private readonly IMtTypeStorage _typeStorage;
        private readonly IMtObjectConverter _converter;
        private readonly IMtObjectsStorage _mtObjectsStorage;
        private readonly List<MtObjectChange> _changes = new List<MtObjectChange>(); 

        public MultitenantRepository(IMtTypeStorageProvider typeStorageProvider, IMtTypeStorage typeStorage, IMtObjectConverter converter, IMtObjectsStorage mtObjectsStorage)
        {
            _typeStorageProvider = typeStorageProvider;
            _typeStorage = typeStorage;
            _converter = converter;
            _mtObjectsStorage = mtObjectsStorage;
        }

        public MtTypeReference FindTypeReference(Type clrType)
        {
            return _typeStorageProvider.FindTypeReference(clrType);
        }

        public MtTypeReference FindTypeReference(Guid typeId)
        {
            return _typeStorageProvider.FindTypeReference(typeId);
        }

        private bool IsManifest(Type clrType)
        {
            CrateManifestType dummy;
            return ManifestDiscovery.Default.TryGetManifestType(clrType, out dummy);
        }

        public MtTypeReference[] ListTypeReferences()
        {
            // return only Manifests
            return _typeStorageProvider.ListTypeReferences().Where(x=>IsManifest(x.ClrType)).ToArray();
        }

        public MtTypePropertyReference[] ListTypePropertyReferences(Guid typeId)
        {
            return _typeStorageProvider.ListTypePropertyReferences(typeId).ToArray();
        }

        public void Add(Manifest instance, string fr8AccountId)
        {
            if (instance == null)
            {
                return;
            }

            var mtType = _typeStorage.ResolveType(instance.GetType(), _typeStorageProvider, true);
            var mtObject = _converter.ConvertToMt(instance, mtType);
            var constraint = BuildKeyPropertyExpression(instance, mtType);

            _changes.Add(new MtObjectChange(mtObject, MtObjectChangeType.Insert, constraint, fr8AccountId));
        }

        public void Add<T>(string fr8AccountId, T instance, Expression<Func<T, object>> uniqueConstraint = null)
            where T : Manifest
        {
            if (instance == null)
            {
                return;
            }

            var mtType = _typeStorage.ResolveType(instance.GetType(), _typeStorageProvider, true);
            var mtObject = _converter.ConvertToMt(instance, mtType);
            var constraint = ConvertToAst(uniqueConstraint, mtType) ?? BuildKeyPropertyExpression(instance, mtType);

            _changes.Add(new MtObjectChange(mtObject, MtObjectChangeType.Insert, constraint, fr8AccountId));
        }

        public void Update(string fr8AccountId, Manifest instance)
        {
            if (instance == null)
            {
                return;
            }

            var mtType = _typeStorage.ResolveType(instance.GetType(), _typeStorageProvider, true);
            var mtObject = _converter.ConvertToMt(instance, mtType);
            var ast = BuildKeyPropertyExpression(instance, mtType);

            if (ast == null)
            {
                throw new InvalidOperationException(string.Format("Primary key for manifest {0} is not defined", instance.GetType()));
            }

            _changes.Add(new MtObjectChange(mtObject, MtObjectChangeType.Update, ast, fr8AccountId));
        }

        public void Update<T>(string fr8AccountId, T instance, Expression<Func<T, bool>> where = null)
             where T : Manifest
        {
            if (instance == null)
            {
                return;
            }

            var mtType = _typeStorage.ResolveType(typeof(T), _typeStorageProvider, true);
            var mtObject = _converter.ConvertToMt(instance, mtType);
            var ast = ConvertToAst(where, mtType) ?? BuildKeyPropertyExpression(instance, mtType);

            if (ast == null)
            {
                throw new InvalidOperationException(string.Format("Primary key for manifest {0} is not defined", instance.GetType()));
            }

            _changes.Add(new MtObjectChange(mtObject, MtObjectChangeType.Update, ast, fr8AccountId));
        }

        public void Delete<T>(string fr8AccountId, Expression<Func<T, bool>> where)
             where T : Manifest
        {
            var mtType = _typeStorage.ResolveType(typeof(T), _typeStorageProvider, false);
            var ast = ConvertToAst(where, mtType);

            _changes.Add(new MtObjectChange(new MtObject(mtType), MtObjectChangeType.Delete, ast, fr8AccountId));
        }

        public void AddOrUpdate(string fr8AccountId, Manifest instance)
        {
            if (instance == null)
            {
                return;
            }

            var mtType = _typeStorage.ResolveType(instance.GetType(), _typeStorageProvider, true);
            var ast = BuildKeyPropertyExpression(instance, mtType);
            var mtObject = _converter.ConvertToMt(instance, mtType);

            _changes.Add(new MtObjectChange(mtObject, MtObjectChangeType.Upsert, ast, fr8AccountId));
        }

        public void AddOrUpdate<T>(string fr8AccountId, T instance, Expression<Func<T, bool>> where = null)
             where T : Manifest
        {
            var mtType = _typeStorage.ResolveType(typeof(T), _typeStorageProvider, true);
            var ast = ConvertToAst(where, mtType) ?? BuildKeyPropertyExpression(instance, mtType);
            var mtObject = _converter.ConvertToMt(instance, mtType);

            _changes.Add(new MtObjectChange(mtObject, MtObjectChangeType.Upsert, ast, fr8AccountId));
        }

        public List<T> Query<T>(string fr8AccountId, Expression<Func<T, bool>> where)
            where T : Manifest
        {
            var mtType = _typeStorage.ResolveType(typeof(T), _typeStorageProvider, false);

            var result = new List<T>();

            if (mtType == null)
            {
               return result;
            }

            foreach (var mtObj in _mtObjectsStorage.Query(fr8AccountId, mtType, ConvertToAst(where, mtType)))
            {
                result.Add((T)_converter.ConvertToObject(mtObj));
            }

            return result;
        }

        public IMtQueryable<T> AsQueryable<T>(string curFr8AccountId)
           where T : Manifest
        {
            return new MtQueryAll<T>(new MtQueryExecutor<T>(this, curFr8AccountId));
        }
        
        private AstNode BuildKeyPropertyExpression(Manifest curManifest, MtTypeDefinition type)
        {
            var pk = curManifest.GetPrimaryKey();

            if (pk.Length == 0)
            {
                return null;
            }

            AstNode root = null;
            var manifestType = curManifest.GetType();

            foreach (var pkProp in pk)
            {
                var member = manifestType.GetProperty(pkProp);

                if (member == null)
                {
                    throw new Exception(string.Format("Failed to find property '{0}' specified as part of primary key. Manifest {1}", pkProp, manifestType));
                }

                var value = member.GetValue(curManifest);
                var mtProp = type.Properties.FirstOrDefault(x => x.Name == pkProp);

                if (mtProp == null)
                {
                    throw new Exception(string.Format("Failed to find MT property '{0}' specified as part of primary key. Manifest {1}", pkProp, manifestType));
                }
                
                var op = new BinaryOpNode(MtBinaryOp.Eq)
                {
                    Left = new LoadFieldNode(mtProp.Index),
                    Right = new LoadConstNode(value)
                };
                
                if (root == null)
                {
                    root = op;
                }
                else
                {
                    root = new BinaryOpNode(MtBinaryOp.And)
                    {
                        Left = root,
                        Right = op
                    };
                }
            }

            return root;
        }

        public void SaveChanges()
        {
            _typeStorageProvider.SaveChanges();

            foreach (var mtObjectChange in _changes)
            {
                switch (mtObjectChange.Type)
                {
                    case MtObjectChangeType.Insert:
                        _mtObjectsStorage.Insert(mtObjectChange.Fr8AccountId, mtObjectChange.Object, mtObjectChange.Constraint);
                        break;

                    case MtObjectChangeType.Update:
                        _mtObjectsStorage.Update(mtObjectChange.Fr8AccountId, mtObjectChange.Object, mtObjectChange.Constraint);
                        break;

                    case MtObjectChangeType.Upsert:
                        _mtObjectsStorage.Upsert(mtObjectChange.Fr8AccountId, mtObjectChange.Object, mtObjectChange.Constraint);
                        break;

                    case MtObjectChangeType.Delete:
                        _mtObjectsStorage.Delete(mtObjectChange.Fr8AccountId, mtObjectChange.Object.MtTypeDefinition, mtObjectChange.Constraint);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Unknown change type: {0}", mtObjectChange.Type));
                }
            }

            _changes.Clear();
        }

        private AstNode ConvertToAst(Expression expression, MtTypeDefinition type)
        {
            return ExpressionToAstConverter.Convert(expression, type);
        }
    }
}

﻿using System.Linq;
using Data.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Assert = NUnit.Framework.Assert;

namespace DockyardTest.Entities
{
    [TestFixture]
    [Category("Action")]
    public class ActionTests : BaseTest
    {
        [Test]
        [Priority(4)]
        public void Action_Add_CanCreateAction()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                
                //SETUP
                //create a customer from fixture data
                var curActionDO = FixtureData.TestAction1();

                //EXECUTE
                uow.ActionRepository.Add(curActionDO);
                uow.SaveChanges();

                //VERIFY
                //check that it was saved to the db
                var savedActionDO = uow.ActionRepository.GetQuery().FirstOrDefault(u => u.Id == curActionDO.Id);
                Assert.NotNull(savedActionDO);
                Assert.AreEqual(curActionDO.Name, savedActionDO.Name);

                var curActionDO2 = FixtureData.TestAction2();

                //EXECUTE
                uow.ActionRepository.Add(curActionDO2);
                uow.SaveChanges();

                //VERIFY
                //check that it was saved to the db
                var savedActionDO2 = uow.ActionRepository.GetQuery().FirstOrDefault(u => u.Id == curActionDO2.Id);
                Assert.NotNull(savedActionDO2);
                Assert.AreEqual(curActionDO2.Name, savedActionDO2.Name);
            }
        }

        // DO-1214
//        [Test,Ignore]
//        [Category("ActionList")]
//                
//        public void ActionList_Add_CanCreateActionList()
//        {
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//              
//                //SETUP
//                //create a customer from fixture data
//                
//                var curProcessNodeTemplateDO = FixtureData.TestProcessNodeTemplateDO1();
//                uow.ProcessNodeTemplateRepository.Add(curProcessNodeTemplateDO);
//                uow.SaveChanges();
//
//                var curActionListDO = FixtureData.TestEmptyActionList();
//                uow.ActionListRepository.Add(curActionListDO);
//                uow.SaveChanges();
//
//                //VERIFY
//                //check that it was saved to the db
//                var savedActionListDO = uow.ActionListRepository.GetQuery().FirstOrDefault(u => u.Id == curActionListDO.Id);
//                Assert.NotNull(savedActionListDO);
//                Assert.AreEqual(curActionListDO.Name, savedActionListDO.Name);
//
//
//                //EXECUTE
//                var curActionListDO2 = FixtureData.TestActionList();
//
//                uow.ActionListRepository.Add(curActionListDO2);
//                uow.SaveChanges();
//
//                //VERIFY
//                //check that it was saved to the db
//                var savedActionListDO2 = uow.ActionListRepository.GetQuery().FirstOrDefault(u => u.Id == curActionListDO2.Id);
//
//                Assert.NotNull(curActionListDO2);
//                Assert.NotNull(savedActionListDO2);
//                Assert.AreEqual(curActionListDO2.Name, savedActionListDO2.Name);
//                Assert.AreEqual(curActionListDO2.Activities.FirstOrDefault().Id,
//                    savedActionListDO2.Activities.FirstOrDefault().Id);
//            }
//        }
    }
}
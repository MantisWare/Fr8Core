﻿using Data.Interfaces;
using Data.Entities;
using Data.Validations;
using FluentValidation;

namespace Data.Repositories
{
    public class ProcessRepository : GenericRepository<ProcessDO>, IProcessRepository
    {
      
        internal ProcessRepository(IUnitOfWork uow)
            : base(uow)
        {
          
        }




    }
    public interface IProcessRepository : IGenericRepository<ProcessDO>
    {
    }



}

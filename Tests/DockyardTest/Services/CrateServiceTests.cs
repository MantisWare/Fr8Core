﻿using Core.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;
using NUnit.Framework;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;

namespace DockyardTest.Services
{
    [TestFixture]
    [Category("CrateService")]
    public class CrateServiceTests : BaseTest
    {
        ICrate _crate;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _crate = ObjectFactory.GetInstance<ICrate>();
        }

        [Test]
        public void Create_CreateNewCrate_ReturnsCrateDTO()
        {
            CrateDTO crateDTO = _crate.Create("Label 1", "Content 1");
            Assert.NotNull(crateDTO);
            Assert.IsNotEmpty(crateDTO.Id);
        }
    }
}

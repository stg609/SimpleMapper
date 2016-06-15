using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SimpleMapper.Tests
{
    [TestClass]
    public class MapperTest
    {
        Mapper _mapper;

        [TestInitialize]
        public void Init()
        {
            _mapper = new Mapper();

        }

        [TestMethod]
        public void MapSimpleModel()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty);

            //Action
            TestVMA result = _mapper.Map<TestModelA, TestVMA>(model);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
        }

        [TestMethod]
        public void MapSimpleModelByFactory()
        {
            //Prepare
            UnityContainer container = new UnityContainer();
            container.RegisterType<IObjectFactory, TestVMAFactory>(typeof(TestVMA).FullName);

            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty);

            //Action
            var result = _mapper.Map<TestModelA, TestVMA>(model, container: container);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual("This is created by TestVMAFactory", result.AStrProperty);
        }

        [TestMethod]
        public void MapSimpleModelByFactoryAndPredicteTrue()
        {
            //Prepare
            UnityContainer container = new UnityContainer();
            container.RegisterType<IObjectFactory, TestVMAFactory>(typeof(TestVMA).FullName);

            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                AStrProperty = "Test"
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AStrProperty, vm => vm.AStrProperty, m => m.AStrProperty == "Test");

            //Action
            var result = _mapper.Map<TestModelA, TestVMA>(model, container: container);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual("This is created by TestVMAFactory", result.AStrProperty);
        }

        [TestMethod]
        public void MapSimpleModelByFactoryAndPredicteFalse()
        {
            //Prepare
            UnityContainer container = new UnityContainer();
            container.RegisterType<IObjectFactory, TestVMAFactory>(typeof(TestVMA).FullName);

            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                AStrProperty = "Test"
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AStrProperty, vm => vm.AStrProperty, m => m.AStrProperty != "Test");

            //Action
            var result = _mapper.Map<TestModelA, TestVMA>(model, container: container);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual(model.AStrProperty, result.AStrProperty);
        }

        [TestMethod]
        public void MapTwoNestedModel()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                ATMBProperty = new TestModelB
                {
                    BDateProperty = DateTime.Now,
                    BIntProperty = 2,
                    BStrProperty = "this is B"
                }
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty)
                .AddMap<TestModelA, TestVMA>(m => m.ATMBProperty, vm => vm.ATVMBProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BDateProperty, vm => vm.BDateProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BIntProperty, vm => vm.BIntProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BStrProperty, vm => vm.BStrProperty);

            //Action
            var result = _mapper.Map<TestModelA, TestVMA>(model);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual(model.ATMBProperty.BDateProperty, result.ATVMBProperty.BDateProperty);
            Assert.AreEqual(model.ATMBProperty.BIntProperty, result.ATVMBProperty.BIntProperty);
            Assert.AreEqual(model.ATMBProperty.BStrProperty, result.ATVMBProperty.BStrProperty);
        }

        [TestMethod]
        public void MapThreeNestedModel()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                ATMBProperty = new TestModelB
                {
                    BDateProperty = DateTime.Now,
                    BIntProperty = 2,
                    BStrProperty = "this is B",
                    BTMCProperty = new TestModelC
                    {
                        CintArrProperty = new int[] { 1, 2, 3 }
                    }
                }
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty)
                .AddMap<TestModelA, TestVMA>(m => m.ATMBProperty, vm => vm.ATVMBProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BDateProperty, vm => vm.BDateProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BIntProperty, vm => vm.BIntProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BStrProperty, vm => vm.BStrProperty)
                .AddMap<TestModelB, TestVMB>(m => m.BTMCProperty, vm => vm.BTVMCProperty)
                .AddMap<TestModelC, TestVMC>(m => m.CintArrProperty, vm => vm.CintArrProperty);

            //Action
            TestVMA result = _mapper.Map<TestModelA, TestVMA>(model);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual(model.ATMBProperty.BDateProperty, result.ATVMBProperty.BDateProperty);
            Assert.AreEqual(model.ATMBProperty.BIntProperty, result.ATVMBProperty.BIntProperty);
            Assert.AreEqual(model.ATMBProperty.BStrProperty, result.ATVMBProperty.BStrProperty);
            Assert.AreEqual(model.ATMBProperty.BTMCProperty.CintArrProperty.Length, result.ATVMBProperty.BTVMCProperty.CintArrProperty.Length);
            Assert.AreEqual(model.ATMBProperty.BTMCProperty.CintArrProperty[1], result.ATVMBProperty.BTVMCProperty.CintArrProperty[1]);
        }

        [TestMethod]
        public void MapNestedModelByFactory()
        {
            //Prepare
            UnityContainer container = new UnityContainer();
            container.RegisterType<IObjectFactory, TestVMAFactory>(typeof(TestVMA).FullName);
            container.RegisterType<IObjectFactory, TestVMBFactory>(typeof(TestVMB).FullName);

            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                ATMBProperty = new TestModelB
                {
                    BDateProperty = DateTime.Now,
                    BIntProperty = 2,
                    BStrProperty = "this is B"
                }
            };

            _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty);

            //Action
            TestVMA result = _mapper.Map<TestModelA, TestVMA>(model, container: container);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual("This is created by TestVMBFactory!", result.ATVMBProperty.BStrProperty);
            Assert.AreEqual(9999999, result.ATVMBProperty.BIntProperty);
        }


        [TestMethod]
        public void MapFlattenModel()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                ATMBProperty = new TestModelB
                {
                    BDateProperty = DateTime.Now,
                    BIntProperty = 2,
                    BStrProperty = "this is B"
                }
            };

            _mapper.AddMap<TestModelA, TestVMAFlatB>(m => m.AFloatProperty, vm => vm.AFloatProperty)
                .AddMap<TestModelA, TestVMAFlatB>(m => m.AIntProperty, vm => vm.AIntProperty)
                .AddMap<TestModelA, TestVMAFlatB>(m => m.ATMBProperty.BDateProperty, vm => vm.BDateProperty)
                .AddMap<TestModelA, TestVMAFlatB>(m => m.ATMBProperty.BIntProperty, vm => vm.BIntProperty)
                .AddMap<TestModelA, TestVMAFlatB>(m => m.ATMBProperty.BStrProperty, vm => vm.BStrProperty);

            //Action
            var result = _mapper.Map<TestModelA, TestVMAFlatB>(model, null);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual(model.ATMBProperty.BDateProperty, result.BDateProperty);
            Assert.AreEqual(model.ATMBProperty.BIntProperty, result.BIntProperty);
            Assert.AreEqual(model.ATMBProperty.BStrProperty, result.BStrProperty);
        }

        [TestMethod]
        public void MapFlattenModelByCustom()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                ATMBProperty = new TestModelB
                {
                    BDateProperty = DateTime.Now,
                    BIntProperty = 2,
                    BStrProperty = "this is B"
                }
            };

            //Custom Mapping
            _mapper.AddMap<TestModelA, TestVMAFlatB>((m, vm) =>
            {
                vm.AFloatProperty = m.AFloatProperty;
                vm.AIntProperty = m.AIntProperty;
                vm.BDateProperty = m.ATMBProperty.BDateProperty;
                vm.BIntProperty = m.ATMBProperty.BIntProperty;
                vm.BStrProperty = m.ATMBProperty.BStrProperty;
            });

            //Action
            var result = _mapper.Map<TestModelA, TestVMAFlatB>(model);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
            Assert.AreEqual(model.ATMBProperty.BDateProperty, result.BDateProperty);
            Assert.AreEqual(model.ATMBProperty.BIntProperty, result.BIntProperty);
            Assert.AreEqual(model.ATMBProperty.BStrProperty, result.BStrProperty);
        }

        [TestMethod]
        public void MapSimpleArray()
        {
            //Prepare
            TestModelC model = new TestModelC
            {
                CintArrProperty = new int[] { 1, 2, 3 }
            };

            _mapper.AddMap<TestModelC, TestVMC>(m => m.CintArrProperty, vm => vm.CintArrProperty);

            //Action
            var result = _mapper.Map<TestModelC, TestVMC>(model);

            //Assert
            Assert.AreEqual(model.CintArrProperty.Length, result.CintArrProperty.Length);
            Assert.AreEqual(model.CintArrProperty[1], result.CintArrProperty[1]);
        }

        [TestMethod]
        public void MapNestedArray()
        {
            //Prepare
            TestModelB model = new TestModelB
            {
                BDateProperty = DateTime.Now,
                BIntProperty = 2,
                BStrProperty = "this is B",
                BTMCProperty = new TestModelC
                {
                    CintArrProperty = new int[] { 1, 2, 3 }
                }
            };

            _mapper.AddMap<TestModelB, TestVMB>(m => m.BDateProperty, vm => vm.BDateProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BIntProperty, vm => vm.BIntProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BStrProperty, vm => vm.BStrProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BTMCProperty, vm => vm.BTVMCProperty)
               .AddMap<TestModelC, TestVMC>(m => m.CintArrProperty, vm => vm.CintArrProperty);

            //Action
            var result = _mapper.Map<TestModelB, TestVMB>(model);

            //Assert
            Assert.AreEqual(model.BDateProperty, result.BDateProperty);
            Assert.AreEqual(model.BIntProperty, result.BIntProperty);
            Assert.AreEqual(model.BStrProperty, result.BStrProperty);
            Assert.AreEqual(model.BTMCProperty.CintArrProperty.Length, result.BTVMCProperty.CintArrProperty.Length);
            Assert.AreEqual(model.BTMCProperty.CintArrProperty[2], result.BTVMCProperty.CintArrProperty[2]);
        }

        [TestMethod]
        public void MapComplexArray()
        {
            //Prepare
            TestModelB model = new TestModelB
            {
                BDateProperty = DateTime.Now,
                BIntProperty = 2,
                BStrProperty = "this is B",
                BTMCProperty = new TestModelC
                {
                    CintArrProperty = new int[] { 1, 2, 3 }
                },
                BTMCArray = new TestModelC[]
                {
                     new TestModelC
                     {
                          CintArrProperty = new int[] { 1, 2, 3 }
                     },
                     new TestModelC
                     {
                          CintArrProperty = new int[] { 4, 5, 6 }
                     } 
                 }
            };

            _mapper.AddMap<TestModelB, TestVMB>(m => m.BDateProperty, vm => vm.BDateProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BIntProperty, vm => vm.BIntProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BStrProperty, vm => vm.BStrProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BTMCProperty, vm => vm.BTVMCProperty)
               .AddMap<TestModelC, TestVMC>(m => m.CintArrProperty, vm => vm.CintArrProperty)
               .AddMap<TestModelB, TestVMB>(m => m.BTMCArray, vm => vm.BTVMCArray);

            //Action
            var result = _mapper.Map<TestModelB, TestVMB>(model);

            //Assert
            Assert.AreEqual(model.BDateProperty, result.BDateProperty);
            Assert.AreEqual(model.BIntProperty, result.BIntProperty);
            Assert.AreEqual(model.BStrProperty, result.BStrProperty);
            Assert.AreEqual(model.BTMCProperty.CintArrProperty.Length, result.BTVMCProperty.CintArrProperty.Length);
            Assert.AreEqual(model.BTMCProperty.CintArrProperty[2], result.BTVMCProperty.CintArrProperty[2]);
            Assert.AreEqual(model.BTMCArray.Length, result.BTVMCArray.Length);
            Assert.AreEqual(model.BTMCArray[1].CintArrProperty.Length, result.BTVMCArray[1].CintArrProperty.Length);
            Assert.AreEqual(model.BTMCArray[1].CintArrProperty[1], result.BTVMCArray[1].CintArrProperty[1]);
        }

        [TestMethod]
        public void AutoMapSimpleModel()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F
            };

            //Action
            var result = _mapper.AutoMap<TestModelA, TestVMA>(model);

            //Assert
            Assert.AreEqual(model.AIntProperty, result.AIntProperty);
            Assert.AreEqual(model.AFloatProperty, result.AFloatProperty);
        }

        public void Serialize()
        {
            //Prepare
            TestModelA model = new TestModelA
            {
                AIntProperty = 1,
                AFloatProperty = 1.1F,
                ATMBProperty = new TestModelB
                {
                    BDateProperty = DateTime.Now,
                    BIntProperty = 2,
                    BStrProperty = "this is B"
                }
            };

            XmlSerializer ser = new XmlSerializer(typeof(TestModelA));
            using (StringWriter sw = new StringWriter())
            {
                ser.Serialize(sw, model);
                string ss = sw.ToString();
            }

            string strsss = @"<aa>
  <a xmlns='http://bb.c'>1</a>
  <AFloatProperty xmlns='http://bb.c'>1.1</AFloatProperty>
  <ATMBProperty xmlns='http://bb.c'>
    <BIntProperty>2</BIntProperty>
    <BDateProperty>2016-06-13T20:46:45.8391467+08:00</BDateProperty>
    <BStrProperty>this is B</BStrProperty>
  </ATMBProperty>
</aa>";
            using (StringReader sr = new StringReader(strsss))
            {
                TestModelA maaa = ser.Deserialize(sr) as TestModelA;
            }

        }
    }

    #region Models for test
    public class TestModelA
    {
        public int AIntProperty { get; set; }
        public float AFloatProperty { get; set; }
        public string AStrProperty { get; set; }
        public TestModelB ATMBProperty { get; set; }
    }

    public class TestModelB
    {
        public int BIntProperty { get; set; }
        public DateTime BDateProperty { get; set; }
        public string BStrProperty { get; set; }

        public TestModelC BTMCProperty { get; set; }
        public TestModelC[] BTMCArray { get; set; }
    }

    public class TestModelC
    {
        public int[] CintArrProperty { get; set; }
    }

    public class TestVMA
    {
        public int AIntProperty { get; set; }
        public float AFloatProperty { get; set; }
        public string AStrProperty { get; set; }
        public TestVMB ATVMBProperty { get; set; }
    }

    public class TestVMB
    {
        public int BIntProperty { get; set; }
        public DateTime BDateProperty { get; set; }
        public string BStrProperty { get; set; }

        public TestVMC BTVMCProperty { get; set; }

        public TestVMC[] BTVMCArray { get; set; }
    }

    public class TestVMC
    {
        public int[] CintArrProperty { get; set; }
    }

    public class TestVMAFlatB
    {
        public int AIntProperty { get; set; }
        public float AFloatProperty { get; set; }
        public int BIntProperty { get; set; }
        public DateTime BDateProperty { get; set; }
        public string BStrProperty { get; set; }
    }

    public class TestVMAFactory : IObjectFactory
    {
        public object InitializeType()
        {
            return new TestVMA
            {
                AFloatProperty = 999999.99F,
                AIntProperty = 999999,
                AStrProperty = "This is created by TestVMAFactory"
            };
        }
    }

    public class TestVMBFactory : IObjectFactory
    {
        public object InitializeType()
        {
            return new TestVMB
            {
                BDateProperty = DateTime.Now,
                BStrProperty = "This is created by TestVMBFactory!",
                BIntProperty = 9999999
            };
        }
    }
    #endregion

}

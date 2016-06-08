using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
        public void MapSimple()
        {
            //Prepare
            TestModel model = new TestModel
            {
                Str = 22,
                Integer = 123
            };
            _mapper.AddMap<TestModel, TestViewModel>(m => Convert.ToString(m.Str), vm => vm.Str)
                .AddMap<TestModel, TestViewModel>(m => m.Integer, vm => vm.Integer);

            //Action
            var result = _mapper.Map<TestModel, TestViewModel>(model, null);

            //Assert
            Assert.AreEqual(model.Str.ToString(), result.Str);
            Assert.AreEqual(model.Integer, result.Integer);
        }

        [TestMethod]
        public void MapSimpleByFactory()
        {
            //Prepare
            UnityContainer container = new UnityContainer();
            container.RegisterType<IObjectFactory, TestViewModelFactory>(typeof(TestViewModel).FullName);

            TestModel model = new TestModel
            {
                Str = 22,
                Integer = 123
            };
            _mapper.AddMap<TestModel, TestViewModel>(m => m.Integer, vm => vm.Integer);

            //Action
            var result = _mapper.Map<TestModel, TestViewModel>(model, null, container);

            //Assert
            Assert.AreEqual("This is created by factory", result.Str);
            Assert.AreEqual(model.Integer, result.Integer);
        }

        [TestMethod]
        public void MapNested()
        {
            //Prepare
            TestNestedModel model = new TestNestedModel
            {
                Arr = new int[] { 1, 2, 3 },
                Str = 22,
                Integer = 123,
                Child = new ViewModelChildModel
                {
                    MyProperty = "This is Child"
                }
            };
            _mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Arr, vm => vm.Arr)
                .AddMap<TestNestedModel, TestViewModel>(m => Convert.ToString(m.Str), vm => vm.Str)
                .AddMap<TestNestedModel, TestViewModel>(m => m.Integer, vm => vm.Integer)
                .AddMap<TestNestedModel, TestViewModel>(m => m.Child.MyProperty, vm => vm.ChildProperty);

            //Action
            var result = _mapper.Map<TestNestedModel, TestViewModel>(model, null);

            //Assert
            Assert.AreEqual(model.Str.ToString(), result.Str);
            Assert.AreEqual(model.Arr[2], result.Arr[2]);
            Assert.AreEqual(model.Integer, result.Integer);
            Assert.AreEqual(model.Child.MyProperty, result.ChildProperty);
        }

        [TestMethod]
        public void MapArray()
        {
            //Prepare
            TestNestedModel model = new TestNestedModel
            {
                Arr = new int[] { 1, 2, 3 },
                Childs = new ViewModelChildModel[]
                {
                    new ViewModelChildModel
                    {
                         MyProperty = "aaa"
                    }
                }
            };

            //Simple Type Array
            _mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Arr, vm => vm.Arr);

            //Complex Type Array
            _mapper.AddMap<ViewModelChildModel, ResultChildModel>(m => m.MyProperty, vm => vm.MyProperty);
            _mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Childs, vm => vm.Childs);

            //Action
            var result = _mapper.Map<TestNestedModel, TestViewModel>(model, null);

            //Assert
            Assert.AreEqual(model.Arr[2], result.Arr[2]);
            Assert.AreEqual(model.Childs.Length, result.Childs.Length);
            Assert.AreEqual(model.Childs[0].MyProperty, result.Childs[0].MyProperty);
        }

        [TestMethod]
        public void MapCustom()
        {
            //Prepare
            TestNestedModel model = new TestNestedModel
            {
                Arr = new int[] { 1, 2, 3 },
                Childs = new ViewModelChildModel[]
                {
                    new ViewModelChildModel
                    {
                         MyProperty = "aaa"
                    },
                     new ViewModelChildModel
                    {
                         MyProperty = "abbbbaa"
                    }
                }
            };

            //Simple Type Array
            _mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Arr, vm => vm.Arr);

            //Custom Mapping
            _mapper.AddMap<TestNestedModel, TestViewModel>((nest, vm) =>
                {
                    vm.Childs = new ResultChildModel[nest.Childs.Length];
                    for (int i = 0; i < nest.Childs.Length; i++)
                    {
                        vm.Childs[i] = new ResultChildModel
                        {
                            MyProperty = nest.Childs[i].MyProperty
                        };
                    }
                });

            //Action
            var result = _mapper.Map<TestNestedModel, TestViewModel>(model, null);

            //Assert
            Assert.AreEqual(model.Arr[2], result.Arr[2]);
            Assert.AreEqual(model.Childs.Length, result.Childs.Length);
            Assert.AreEqual(model.Childs[1].MyProperty, result.Childs[1].MyProperty);
        }
    }

    public class TestModel
    {
        public int Integer { get; set; }
        public int Str { get; set; }
        public int[] Arr { get; set; }
    }

    public class TestViewModel
    {
        public int Integer { get; set; }
        public string Str { get; set; }
        public int[] Arr { get; set; }
        public string ChildProperty { get; set; }
        public ResultChildModel[] Childs { get; set; }
    }

    public class ViewModelChildModel
    {
        public string MyProperty { get; set; }
    }


    public class ResultChildModel
    {
        public string MyProperty { get; set; }
    }


    public class TestNestedModel
    {
        public int Integer { get; set; }
        public int Str { get; set; }
        public int[] Arr { get; set; }
        public ViewModelChildModel Child { get; set; }
        public ViewModelChildModel[] Childs { get; set; }
    }

    public class TestViewModelFactory : IObjectFactory
    {
        public object InitializeType()
        {
            return new TestViewModel
            {
                Str = "This is created by factory"
            };
        }
    }
}

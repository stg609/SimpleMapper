using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SimpleMapper.Tests
{

    [TestClass]
    public class MapperTest
    {
        [TestInitialize]
        public void Init()
        {
            Mapper.ClearMaps();
        }

        [TestMethod]
        public void MapSimple()
        {
            //Prepare
            TestModel model = new TestModel
            {
                Arr = new int[] { 1, 2, 3 },
                Str = 22,
                Integer = 123
            };
            Mapper.AddMap<TestModel, TestViewModel>(m => m.Arr, vm => vm.Arr);
            Mapper.AddMap<TestModel, TestViewModel>(m => Convert.ToString(m.Str), vm => vm.Str);
            Mapper.AddMap<TestModel, TestViewModel>(m => m.Integer, vm => vm.Integer);

            //Action
            var result = Mapper.Map<TestModel, TestViewModel>(model, null);

            //Assert
            Assert.AreEqual(model.Str.ToString(), result.Str);
            Assert.AreEqual(model.Arr[2], result.Arr[2]);
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
                Arr = new int[] { 1, 2, 3 },
                Str = 22,
                Integer = 123
            };
            Mapper.AddMap<TestModel, TestViewModel>(m => m.Arr, vm => vm.Arr);
            //Mapper.AddMap<TestModel, TestViewModel>(m => Convert.ToString(m.Str), vm => vm.Str);
            Mapper.AddMap<TestModel, TestViewModel>(m => m.Integer, vm => vm.Integer);

            //Action
            var result = Mapper.Map<TestModel, TestViewModel>(model, null, container);

            //Assert
            Assert.AreEqual("This is created by factory", result.Str);
            Assert.AreEqual(model.Arr[2], result.Arr[2]);
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
                Child = new ChildModel
                {
                    MyProperty = "This is Child"
                }
            };
            Mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Arr, vm => vm.Arr);
            Mapper.AddMap<TestNestedModel, TestViewModel>(m => Convert.ToString(m.Str), vm => vm.Str);
            Mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Integer, vm => vm.Integer);
            Mapper.AddMap<TestNestedModel, TestViewModel>(m => m.Child.MyProperty, vm => vm.ChildProperty);

            //Action
            var result = Mapper.Map<TestNestedModel, TestViewModel>(model, null);

            //Assert
            Assert.AreEqual(model.Str.ToString(), result.Str);
            Assert.AreEqual(model.Arr[2], result.Arr[2]);
            Assert.AreEqual(model.Integer, result.Integer);
            Assert.AreEqual(model.Child.MyProperty, result.ChildProperty);
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
        public string[] ChildProperties { get; set; }
    }

    public class ChildModel
    {
        public string MyProperty { get; set; }
    }

    public class TestNestedModel
    {
        public int Integer { get; set; }
        public int Str { get; set; }
        public int[] Arr { get; set; }
        public ChildModel Child { get; set; }
        public ChildModel[] Childs { get; set; }
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

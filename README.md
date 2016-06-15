# SimpleMapper
SimpleMapper is used to map from source to target. If the target is null, the mapper will try to find the corresponding IObjectFactory to create a new instance for the target (If the target is not existed, it will use the default constructor of the target type to create one).

# Getting Startted
### Simplest Demo 
**1. Prepare Two models**

    //Model
    public class TestModelA
    {
        public int AIntProperty { get; set; }
        public float AFloatProperty { get; set; }
        public string AStrProperty { get; set; }
    }

    //View Model
    public class TestVMA
    {
        public int AIntProperty { get; set; }
        public float AFloatProperty { get; set; }
        public string AStrProperty { get; set; }
    }
    
**2. Create a Mapper Instance**

    Mapper _mapper = new Mapper();

**3. Add Mapping Rules**

    _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
        .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty);

**4. Map**

    TestModelA model = new TestModelA
    {
        AIntProperty = 1,
        AFloatProperty = 1.1F
    };
    TestVMA result = _mapper.Map<TestModelA, TestVMA>(model);
    
After mapping, the **_result.AIntProperty_** and **_result.AFloatProperty_** will be the same as the model.

Above demo will create a brand new instance of the target type, we can also pass in a existed instance of the target.

**5. Map to an existed instance**
    TestModelA model = new TestModelA
    {
        AIntProperty = 1,
        AFloatProperty = 1.1F
    };
    TestVMA viewModel = new TestVMA
    {
        AIntProperty = 2,
        AStrProperty = "This is VMA"
    };
    TestVMA result = _mapper.Map<TestModelA, TestVMA>(model, viewModel);
    
After mapping, the **_result.AIntProperty_** and **_result.AFloatProperty_** will be the same as the model.But the **_result.AStrProperty_** will be "This is VMA".

### Simple Model with Factory
If we want to use a factory to create the target instance, we can provide with a Factory.

In this demo, we still use **_TestModelA_** and **_TestVMA_**. However, we'll use a Factory to create the **_TestVMA_**.

**1. Prepare Factory**

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

**2. Register the factory**

SimpleMapper use **Unity** as the IoC to create the Instance.

    UnityContainer container = new UnityContainer();
    //Please use type's name as the parameter
    container.RegisterType<IObjectFactory, TestVMAFactory>(typeof(TestVMA).FullName); 

**3. Add Mapping Rules**

    _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
        .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty);
        
**4. Map**

    TestVMA result = _mapper.Map<TestModelA, TestVMA>(model, container: container);
    
After mapping, the **_result.AIntProperty_** and **_result.AFloatProperty_** will be the same as the model.But the **_result.AStrProperty_** will be "This is created by TestVMAFactory".

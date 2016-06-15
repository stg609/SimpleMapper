# SimpleMapper
SimpleMapper is used to map from source to target. If the target is null, the mapper will try to find the corresponding IObjectFactory to create a new instance for the target (If the target is not existed, it will use the default constructor of the target type to create one).

# Getting Started

Before using Mapper, we need create a Mapper instance firstly.
    Mapper _mapper = new Mapper();
    

## Important Methods

**AddMap**

    Mapper AddMap<TSource, TTarget>(sourceProperty, targetProperty, useDefaultValue);

This method is used to create the mapping rule between two models. 
e.g.

    _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
    
As the method returns Mapper object, we can contiune to invoke other methods afterwards.

*useDefaultValue* is used to indicate wheather the default value of the property (by Factory) will be used.

**Map**

    TTarget Map<TSource, TTarget>(TSource sourceObj, TTarget targetObj = null, UnityContainer container = null)

This method is used to map from source object to target object according to the mapping rules.


## Demos

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

**2. Add Mapping Rules**

    _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
        .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty);

**3. Map**

    TestModelA model = new TestModelA
    {
        AIntProperty = 1,
        AFloatProperty = 1.1F
    };
    TestVMA result = _mapper.Map<TestModelA, TestVMA>(model);
    
After mapping, the **_result.AIntProperty_** and **_result.AFloatProperty_** will be the same as the model.

Above demo will create a brand new instance of the target type, we can also pass in a existed instance of the target.

**4. Map to an existed instance**
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


### Complex Model

**1. Prepare Two models**

    //Model
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

    //View Model
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
    
**2. Add Mapping Rules**

    _mapper.AddMap<TestModelA, TestVMA>(m => m.AFloatProperty, vm => vm.AFloatProperty)
        .AddMap<TestModelA, TestVMA>(m => m.AIntProperty, vm => vm.AIntProperty)
        .AddMap<TestModelA, TestVMA>(m => m.ATMBProperty, vm => vm.ATVMBProperty)
        
        .AddMap<TestModelB, TestVMB>(m => m.BDateProperty, vm => vm.BDateProperty)
        .AddMap<TestModelB, TestVMB>(m => m.BIntProperty, vm => vm.BIntProperty)
        .AddMap<TestModelB, TestVMB>(m => m.BStrProperty, vm => vm.BStrProperty)
        .AddMap<TestModelB, TestVMB>(m => m.BTMCProperty, vm => vm.BTVMCProperty)
        
        .AddMap<TestModelC, TestVMC>(m => m.CintArrProperty, vm => vm.CintArrProperty);
        
**3. Map**

    TestVMA result = _mapper.Map<TestModelA, TestVMA>(model);
    
After mapping, each property of the result will the same as the model.


### Map Complex Model to Flattened Model

In most cases, the ViewModel should be as simple as possible. Just as the following model shows:

    public class TestVMAFlatB
    {
        public int AIntProperty { get; set; }
        public float AFloatProperty { get; set; }
        public int BIntProperty { get; set; }
        public DateTime BDateProperty { get; set; }
        public string BStrProperty { get; set; }
    }
    
It's very easy to map from complex Model TestModelA to TestVMAFlatB.

**1. Add Mapping Rules**

    _mapper.AddMap<TestModelA, TestVMAFlatB>(m => m.AFloatProperty, vm => vm.AFloatProperty)
        .AddMap<TestModelA, TestVMAFlatB>(m => m.AIntProperty, vm => vm.AIntProperty)
        .AddMap<TestModelA, TestVMAFlatB>(m => m.ATMBProperty.BDateProperty, vm => vm.BDateProperty)
        .AddMap<TestModelA, TestVMAFlatB>(m => m.ATMBProperty.BIntProperty, vm => vm.BIntProperty)
        .AddMap<TestModelA, TestVMAFlatB>(m => m.ATMBProperty.BStrProperty, vm => vm.BStrProperty);
        
**2. Map**

    TestVMAFlatB result = _mapper.Map<TestModelA, TestVMAFlatB>(model, null);
    
After mapping, all those properties in the rules should be the same as the model.


### Custom Mapping

If the way creating mapping rule is not sufficient, we can use custom way to create mapping rule. Let's use the custom way to add mapping rule between **_TestModelA_** and **_TEstVMAFlatB_**.

    //Custom Mapping
    _mapper.AddMap<TestModelA, TestVMAFlatB>((m, vm) =>
    {
        vm.AFloatProperty = m.AFloatProperty;
        vm.AIntProperty = m.AIntProperty;
        vm.BDateProperty = m.ATMBProperty.BDateProperty;
        vm.BIntProperty = m.ATMBProperty.BIntProperty;
        vm.BStrProperty = m.ATMBProperty.BStrProperty;
    });
    
The effect is the same as above demo.

## Restrictions

**v0.9**

-Only support array, List or other Enumerations are not supported

-AutoMapper is not fully supported

# OptionSettingItems Input Validation Design Document
_DOTNET-4952_

## Summary
Need mechanism to indicate an `OptionSettingItem` is required (ie non empty value) or has additional input validations:
 - less than 64 characters
 - no whitespace characters
 - no non-ascii characters
 - value is from list of acceptable values
 - is positive number
 - is number within bounds

Deployment should be blocked if there are any OptionSettingItems with invalid values.

## Proposed Design

Unqiue validation classes will be created and will implement a new interface:

```csharp
public interface IOptionSettingItemValidator
{
    OptionSettingItemValidationResult Validate(object input);
}

public class OptionSettingItemValidationResult
{
    public bool IsValid { get; set; }
    public string ValidationFailedMessage { get;set; }
}
```

OptionSettingItems will then contain a collection of 0 or more `IOptionSettingItemValidator`s:

```csharp
public class OptionSettingItem
{
     public List<IOptionSettingItemValidator> Validators { get; set; }
}
```

### Validation

`OptionSettingItem.SetValueOverride` will be updated to validate the override value by passing it to each Validator.  If any validator indicates the value is not valid, an exception is thrown:

```csharp
public void SetValueOverride(object valueOverride)
{
    foreach (var validator in this.Validators)
    {
        var result = validator.Validate(valueOverride);
        if (!result.IsValid)
            throw new ValidationFailedException
            {
                ValidationResult = result
            };
    }

    // value is saved
}
```

### Serialization of Recipes

`RecipeDefinition`s are stored as JSON and deserialized from _*.recipe_ files.  Therefore the Validators must also be JSON serializable.  To faciliate polymorphism, recipe JSON will need to define the type of the validator.

In the example below, the **Example Setting** item has two Validators, `RequiredValidator` and `RegexValidator`:

```JSON
// Minimal Recipe to highlight OptionSettingsItem.Validators
{  
  "Name": "Example Recipe",  
  "OptionSettings": [
    {      
      "Name": "Example Setting",
      "Type": "String",
      "DefaultValue": null,
      "Validators": [
        {
          "ValidatorType": "Regex",
          "Configuration" : {
              "Regex": "[a-zA-Z]{3,20}",
              "AllowEmptyString": true,
              "ValidationFailedMessage": "Letters only, 3 to 20 characters in length"
          }
        },
        {
          "ValidatorType": "Regex",
          "Configuration" : {
              "Regex": "[a-zA-Z]{3,20}",
              "AllowEmptyString": true,
              "ValidationFailedMessage": "Letters only, 3 to 20 characters in length"
          }
        }
      ],
      "AllowedValues": [],
      "ValueMapping": {}
    }
  ],
  "RecipePriority": 0
}
```

This requires the recipe to be deserialzied using customized [TypeNameHandling](https://www.newtonsoft.com/JSON/help/html/T_Newtonsoft_JSON_TypeNameHandling.htm):

```csharp
var settings = new JSONSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Auto
};
var recipe = JSONConvert.DeserializeObject<RecipeDefintion>(JSON, settings);
```

### Validator Configuration

Validator can declare any additional configuration they need in as Properties in the validator class.  And, as the validator is deserialized from JSON, any Property can be customized inside the recipe.  For examle:

```csharp
public class RangeValidator : IOptionSettingItemValidator
{
    public int Min { get; set; } = int.MinValue;
    public int Max { get;set; } = int.MaxValue;

    public string ValidationFailedMessage { get; set; } =
        "Value must be greater than or equal to {{Min}} and less than or equal to {{Max}}";
}
```
#### Message Customization

Each validator is responsible for rendering a validation failed message and can control how it allows recipe authors to customize the message.

In the `RangeValidator` example above, a recipe author can customize `ValidationFailedMessage`.  Additionally, `RangeValidator` suppots two replacement tokens `{{Min}}` and `{{Max}}`.  This allows a recipe author greater flexability:

```JSON
{
    "$type": "AWS.Deploy.Common.Recipes.RangeValidator, AWS.Deploy.Common",
    "Min": "2",
    "Min": "10",
    "ValidationFailedMessage": "Setting can not be more than {{Max}}"
}
```

### Dependencies

Because Validators will be deserialized as part of a `RecipeDefinition` they need to have parameterless constructors and therefore can't use Constructor Injection.

Validators are currently envisoned to be relatively simple to the point where they shouldn't need any dependencies.  If dependencies in are needed in the future, we can explore adding an `Initialize` method that uses the ServiceLocation (anti-)pattern:

```csharp
public interface IOptionSettingItemValidator
{
    /// <summary>
    /// One possibile solution if we need to create a Validator that needs 
    /// dependencies.
    /// </summary>
    void Initialize(IServiceLocator serviceLocator);
}
```

### Extensability

This design would facilitate validators being defined in external code; assuming that the project as a whole allows recipe authors to include 3rd party assemblies and the 3rd party assembly is already loaded.  If those preconditions are met, a recipe author can, in JSON, reference any validator type in an assembly that is loaded by the tool.

#### Recipe Schema

Any 3rd party Validators will not be added to the `aws-deploy-recipe-schema.JSON` file.

### Allowed Values / Value Mapping

`OptionSettingItem` defines:

```csharp
public class OptionSettingItem
{
    /// <summary>
    /// The allowed values for the setting.
    /// </summary>
    public IList<string> AllowedValues { get; set; } = new List<string>();

    /// <summary>
    /// The value mapping for allowed values. The key of the dictionary is what is sent to services
    /// and the value is the display value shown to users.
    /// </summary>
    public IDictionary<string, string> ValueMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // additional properties not shown
}
```

I considered migrating `AllowedValues` to become a Validator, however `AllowedValues` and `ValueMapping` are tightly intergrated into UIs in order to render custom UI prompts, like the one below, that I decided they should remain as is and not be ported to a Validator:

```
Task CPU:
The number of CPU units used by the task. See the following for details on CPU values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
1: 256 (.25 vCPU) (default)
2: 512 (.5 vCPU)
3: 1024 (1 vCPU)
4: 2048 (2 vCPU)
5: 4096 (4 vCPU)
Choose option (default 1):
```

## WIP

- Ideally keep validation targeted to just the incoming value.
- Support validating the entire "Recommendation" as a separate level of validation.
- support validation warnings?

### Validation Scenarios

#### CPU & Memory Pair

Certain memory configurations are only available based on certain CPU configurations and vice versa.  For example, selecting 0.25GB of memory may only be valid if CPU is below 2vCPU.

**Design:**
- OptionSettingsItems will use `OptionSettingItem.AllowedValues` to restrict what the user can select for vCPU and Memory.  However, it will not have context to enforce limitations based on what has been selected for CPU.
- UI can optionally implement a custom TypeHint that can restrict which value pairs are recommended and has access to the full `Recommendation` object.
- A `MemoryCpuRecommendationValidator` will run after the user has indicated they have no more configuration changes and wishes to deploy.  It will be able to access both CPU and Memory `OptionSettingItem` values to ensure they are compatible.
- _Justification:_ The validation system should not force the UX into a state where it is impossible to select a value.  For example, say the a 8 vCPU config requires at least 16 GB memory.  The 16 GB memory requires at least 8 vCPU.  The default values are 0.25 vCPU and 1 GB Memory. If we performed validation at the `OptionSettingItem` level, it would not be possible to change CPU to 8 and Memory to 16GB, as a change to CPU would always be incompatible with the existing Memory selection, and vice versa.

#### Region limited EC2 Instances

Not every AWS Region supports EC2 Instance Type.  Additionanlly, this list is dynamic as new EC2 instance types are introduced in a subset of Regions, or existing Regions get new capabilities.

**Design:**

- InstanceType TypeHint will present users with values valid for selected Region.
- The `InstanceTypeOptionSettingItemValidator` will only validate that the EC2 type selected is in a known list; it will not have the context of which Region has been selected.
- The `InstanceTypeRecommendationValidator` has access to the `OrchestratorSession` and can see which 
Region is targetd.  It can then provide a validation error if the selected EC2 InstanceType is not available in the current region.
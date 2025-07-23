# PhysicsStatistics.cs Bug Fix Plan

## Issues Identified

The following issues were identified in `Source\Core\Physics\Elements\PhysicsStatistics.cs` lines 43-45:

1. **Syntax Error**: Extra space between `get;` and `set` in the `IsFalling` property definition:
   ```csharp
   public bool IsFalling { get; set ; }
   ```

2. **Logical Dependency Not Enforced**: The comment indicates that whenever `IsFalling` is set to true, `CancelHorizontalMotion` must also be set to true, but this dependency is not enforced in code.

3. **Documentation Clarity**: The current comment could be improved to better explain the relationship between these properties.

## Proposed Solution

Implement a custom setter with a backing field to enforce the dependency between `IsFalling` and `CancelHorizontalMotion`:

```csharp
// Before:
/// <summary>
/// Whether the pixel is currently falling due to gravity
/// whenever IsFalling is set to true, cancelHorizontal motion must also be set to true otherwise the object won't start falling
/// </summary>
public bool IsFalling { get; set ; }

// After:
private bool _isFalling;

/// <summary>
/// Whether the pixel is currently falling due to gravity.
/// When set to true, automatically sets CancelHorizontalMotion to true as well
/// to ensure proper falling behavior.
/// </summary>
public bool IsFalling
{
    get => _isFalling;
    set
    {
        _isFalling = value;
        if (value)
        {
            CancelHorizontalMotion = true;
        }
    }
}
```

## Benefits of this Solution

1. **Fixes Syntax Error**: Removes the extra space between `get;` and `set`.
2. **Enforces Dependency**: Automatically sets `CancelHorizontalMotion` to true when `IsFalling` is set to true.
3. **Improves Documentation**: Updates the comment to clearly explain what happens when the property is set.
4. **Maintains API Compatibility**: External code can continue to use the property as before.

## Implementation Steps

1. Add a private backing field `_isFalling`
2. Implement the custom getter and setter for `IsFalling`
3. Update the property's documentation
4. Test to ensure the dependency is properly enforced

## Notes for Implementation

Since `PhysicsStatistics` is a struct, it's important to note that any modifications to `CancelHorizontalMotion` within the setter will only affect the current instance. If this struct is copied elsewhere, the dependency won't be enforced on the copy. This is an inherent limitation of structs in C#.
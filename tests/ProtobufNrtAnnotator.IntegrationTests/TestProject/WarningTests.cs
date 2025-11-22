using System.Linq;

namespace TestProject;

public class WarningTests
{
    public void TriggerNullabilityWarnings()
    {
        var msg = new TestMessage();
        
        // CS8625: Cannot convert null literal to non-nullable reference type
        msg.Name = null;
        msg.Email = null;
        
        // CS8602: Dereference of a possibly null reference
        msg.Details.Description.Contains("test");
        
        // CS8625: Null dictionary key
        msg.Metadata[null] = null;
        
        // CS8602: Dereference of wrapper type that could be null
        msg.Nickname.CompareTo("test");
    }
}

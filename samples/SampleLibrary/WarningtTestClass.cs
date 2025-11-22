using System.Linq;

namespace SampleLibrary;

public class WarningTestClass
{
    public void Thing()
    {
        // The following code should produce nullability warnings.

        var msg = new ComplexMessage();
        msg.Email = null;
        msg.Attributes[null]= null;
        msg.Notes.First().CompareTo("x");
        msg.Nested.Description.Contains("x");
    }
}
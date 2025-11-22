using System.Linq;

namespace TestLibrary;

public class Class
{
    public void Thing()
    {
        var msg = new ComplexMessage();
        msg.Email = null;
        msg.Attributes[null]= null;
        msg.Children[1] = null;

        msg.Notes.First().CompareTo("x");
    }
}
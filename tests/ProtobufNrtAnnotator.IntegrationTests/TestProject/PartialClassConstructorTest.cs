namespace TestProject;

public partial class MsgWithOptionalFields
{
    public MsgWithOptionalFields(string foo)
    {
        this.A = foo;
        // A warning should _not_ be emitted even though the optional bytes field is not initialized 
    }
}

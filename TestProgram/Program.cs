using TestProgram.Protos;

var parent = new Parent();

// This line should produce a warning because Child can be null, 
// but the generated code likely doesn't annotate it as nullable.
Console.WriteLine(parent.Child.Name);

// This should also warn
parent.Child.Name = null;

// Similarly for the string wrapper
if (parent.Child != null)
{
    // Nickname is a StringValue, which is a message, so it can be null.
    // But generated property probably isn't nullable.
    Console.WriteLine(parent.Child.Nickname);
}

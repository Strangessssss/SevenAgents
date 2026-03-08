namespace SevenAgents.Messages;

public class Message(string content, string sender)
{
    public string Content { get; set; } = content;
    public string Sender { get; set; } = sender;
}

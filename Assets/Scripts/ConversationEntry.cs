using System;

[Serializable] public class ConversationEntry
{
    public string role; // "user" または "model"
    public string text;

    public ConversationEntry(string role, string text)
    {
        this.role = role;
        this.text = text;
    }
}
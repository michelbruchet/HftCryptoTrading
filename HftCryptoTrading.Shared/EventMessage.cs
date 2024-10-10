using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute;
public static class EventMessageExts
{
    public static void SetPayload<T>(this EventMessage eventMessage, T payload)
    {
        eventMessage.Payload = MessagePackSerializer.Serialize(payload);
    }

    // Méthode pour désérialiser le Payload
    public static T GetPayload<T>(this EventMessage message)
    {
        return MessagePackSerializer.Deserialize<T>(message.Payload);
    }
}

[MessagePackObject]
public class EventMessage()
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)]
    public string Namespace { get; set; }
    [Key(2)]
    public string EventName { get; set; }
    [Key(3)]
    public byte[] Payload { get; set; } 

    public EventMessage(Guid id, string @namespace, string eventName):this()
    {
        Id = id;
        Namespace = @namespace;
        EventName = eventName;
    }
}

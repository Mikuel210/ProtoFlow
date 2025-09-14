namespace SDK;

[AttributeUsage(AttributeTargets.Class)]
public class InstanceNameAttribute(string name) : Attribute { public string Name => name; }

[AttributeUsage(AttributeTargets.Class)]
public class InstanceDescriptionAttribute(string description) : Attribute { public string Description => description; }

[AttributeUsage(AttributeTargets.Class)]
public class CanClientOpenAttribute(bool canClientOpen) : Attribute { public bool CanClientOpen => canClientOpen; }

[AttributeUsage(AttributeTargets.Class)]
public class CanClientCloseAttribute(bool canClientClose) : Attribute { public bool CanClientClose => canClientClose; }

[AttributeUsage(AttributeTargets.Class)]
public class NotifyOnOpenAttribute(bool notifyOnOpen) : Attribute { public bool NotifyOnOpen => notifyOnOpen; }

[AttributeUsage(AttributeTargets.Class)]
public class ShowOnClientAttribute(bool showOnClient) : Attribute { public bool ShowOnClient => showOnClient; }

[AttributeUsage(AttributeTargets.Class)]
public class SerializeUIAttribute(bool serializeUI) : Attribute { public bool SerializeUI => serializeUI; }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class InstanceStorageAttribute : Attribute;
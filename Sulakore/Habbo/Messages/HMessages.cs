using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Sulakore.Habbo.Web;
using System.IO;

namespace Sulakore.Habbo.Messages
{
    public abstract class HMessages : IEnumerable<HMessage>
    {
        private readonly string _section;
        private readonly Dictionary<ushort, HMessage> _byId;
        private readonly Dictionary<string, HMessage> _byName, _byHash;

        public bool IsOutgoing { get; }
        public int Count => _byId.Count;

        public HMessage this[ushort id] => GetMessage(id);
        public HMessage this[string identifier] => GetMessage(identifier);

        public HMessages(bool isOutgoing)
            : this(isOutgoing, 0)
        { }
        public HMessages(bool isOutgoing, int capacity)
        {
            _section = GetType().Name;
            _byId = new Dictionary<ushort, HMessage>(capacity);
            _byName = new Dictionary<string, HMessage>(capacity);
            _byHash = new Dictionary<string, HMessage>(capacity);

            IsOutgoing = isOutgoing;
        }
        public HMessages(bool isOutgoing, IList<HMessage> messages)
            : this(isOutgoing, messages.Count)
        {
            foreach (HMessage message in messages)
            {
                _byId.Add(message.Id, message);
                _byHash.TryAdd(message.Hash, message); //TODO:
                if (!string.IsNullOrWhiteSpace(message.Name))
                {
                    _byName.Add(message.Name, message);

                    PropertyInfo property = GetType().GetProperty(message.Name);
                    property?.SetValue(this, message);
                }
            }
        }

        public HMessages(HGame game, string identifiersPath, bool isOutgoing)
            : this(isOutgoing)
        {
            Load(game, identifiersPath);
        }

        public void Load(HGame game, string identifiersPath)
        {
            _byId.Clear();
            _byName.Clear();
            _byHash.Clear();
            using (var input = new StreamReader(identifiersPath))
            {
                bool isInSection = false;
                while (!input.EndOfStream)
                {
                    string line = input.ReadLine();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        isInSection = (line == ("[" + _section + "]"));
                    }
                    else if (isInSection)
                    {
                        string[] values = line.Split('=');
                        string name = values[0].Trim();
                        string hash = values[1].Trim() + (IsOutgoing ? "MessageComposer" : "MessageEvent");
                        var id = ushort.MaxValue;
                        if (game.Messages.TryGetValue(hash, out List<MessageItem> messages) && messages.Count == 1)
                        {
                            id = messages[0].Id;
                            if (!_byHash.ContainsKey(hash))
                            {
                                _byHash.Add(hash, new HMessage(id, IsOutgoing, hash, name, null));
                            }
                        }

                        if (id != ushort.MaxValue)
                        {
                            _byId[id] = new HMessage(id, IsOutgoing, hash, name, null);
                        }
                        _byName[name] = new HMessage(id, IsOutgoing, hash, name, null);
                        GetType().GetProperty(name)?.SetValue(this, new HMessage(id, IsOutgoing, hash, name, null));
                    }
                }
            }
        }

        public void Remove(HMessage message)
        {
            _byId.Remove(message.Id);
            if (string.IsNullOrWhiteSpace(message.Hash))
            {
                _byHash.Remove(message.Hash);
            }
            if (!string.IsNullOrWhiteSpace(message.Name))
            {
                _byName.Remove(message.Name);
                GetType().GetProperty(message.Name)?.SetValue(this, null);
            }
        }
        public void AddOrUpdate(HMessage message)
        {
            message.IsOutgoing = IsOutgoing;

            _byId.TryAdd(message.Id, message);
            if (!string.IsNullOrWhiteSpace(message.Name))
            {
                _byName.TryAdd(message.Name, message);
                GetType().GetProperty(message.Name).SetValue(this, message);
            }
            if (!string.IsNullOrWhiteSpace(message.Hash))
            {
                _byHash.TryAdd(message.Hash, message);
            }
        }

        public HMessage GetMessage(ushort id)
        {
            _byId.TryGetValue(id, out HMessage message);
            return message;
        }
        public HMessage GetMessage(string identifier)
        {
            if (_byHash.TryGetValue(identifier, out HMessage namedMessage)) return namedMessage;
            if (_byName.TryGetValue(identifier, out HMessage hashedMessage)) return hashedMessage;
            return null;
        }

        public string GetName(ushort id)
        {
            _byId.TryGetValue(id, out HMessage msg);
            return msg.Name;
        }

        public string GetHash(ushort id)
        {
            _byId.TryGetValue(id, out HMessage msg);
            return msg.Hash;
        }

        public bool IsOut(ushort id)
        {
            _byId.TryGetValue(id, out HMessage msg);
            return msg.IsOutgoing;
        }


        IEnumerator IEnumerable.GetEnumerator() => _byId.Values.GetEnumerator();
        IEnumerator<HMessage> IEnumerable<HMessage>.GetEnumerator() => _byId.Values.GetEnumerator();
    }
}
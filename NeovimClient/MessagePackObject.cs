using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neovim
{
    public class MessagePackObjectDictionary : Dictionary<MessagePackObject, MessagePackObject>
    {
        public MessagePackObjectDictionary() : base()
        {
            
        }

        public MessagePackObjectDictionary(int capacity) : base(capacity)
        {
            
        }
    }

    public class MessagePackObject
    {
        private MsgPack.MessagePackObject _msgPackObject;

        public MessagePackObject(MsgPack.MessagePackObject msgPackObject)
        {
            _msgPackObject = msgPackObject;
        }

        public object ToObject()
        {
            return _msgPackObject.ToObject();
        }

        internal MsgPack.MessagePackExtendedTypeObject AsMessagePackExtendedTypeObject()
        {
            return _msgPackObject.AsMessagePackExtendedTypeObject();
        }

        public byte[] AsBinary()
        {
            return _msgPackObject.AsBinary();
        }

        public sbyte AsSByte()
        {
            return _msgPackObject.AsSByte();
        }

        public bool AsBoolean()
        {
            return _msgPackObject.AsBoolean();
        }

        public long AsInteger()
        {
            return _msgPackObject.AsInt64();
        }

        public double AsFloat()
        {
            return _msgPackObject.AsDouble();
        }

        public string AsString()
        {
            return _msgPackObject.AsString();
        }

        public string AsString(Encoding encoding)
        {
            return _msgPackObject.AsString(encoding);
        }

        public IList<MessagePackObject> AsList()
        {
            var internalList =  _msgPackObject.AsList();
            return internalList.Select(x => new MessagePackObject(x)).ToList();
        }

        public MessagePackObjectDictionary AsDictionary()
        {
            var internalDict =  _msgPackObject.AsDictionary();
            var dict = new MessagePackObjectDictionary(internalDict.Count);
            foreach (var keyValuePair in internalDict)
                dict.Add(new MessagePackObject(keyValuePair.Key), new MessagePackObject(keyValuePair.Value));
            return dict;
        }
    }
}

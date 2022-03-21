using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservableDB.Model
{
    /// <summary>
    /// 
    /// </summary>
    public struct DBEntity : IEqualityComparer<DBEntity>
    {
        public int ID { get;}
        public bool Flag { get;}
        public string Data { get;}

        public DBEntity(int id, bool flag, string data) => (ID, Flag, Data) = (id, flag, data);
        public override string ToString() => $"{Data} ({Flag})";

        public bool Equals(DBEntity x, DBEntity y) => x.ID == y.ID;
        public int GetHashCode([DisallowNull] DBEntity obj) => obj.ID.GetHashCode();
    }
}

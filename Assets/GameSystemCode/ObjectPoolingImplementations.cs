using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackSystem;

namespace ObjectPoolingImplementations {
    public class SimpleCategoricalObjectPool<T> : ICategoricalObjectPool<T> {
        private IObjectPool<T>[] subPools;

        public SimpleCategoricalObjectPool(int maxKey, IObjectPool<T>[] subPools) {
            if (subPools.Length != maxKey + 1) { throw new ArgumentException("SimpleCategoricalObjectPool was passed an incorrect number of subPools; must match maxKey!"); }
            MaxKey = maxKey;
            this.subPools = subPools;
        }

        public int MaxKey { get; }

        public T GetObject(int typeId) {
            CheckTypeId(typeId);
            return subPools[typeId].GetObject();
        }

        public bool PoolObject(T objectToDeactivate, int typeId) {
            CheckTypeId(typeId);
            return subPools[typeId].PoolObject(objectToDeactivate);
        }

        private void CheckTypeId(int t) {
            if (t < 0 || t > MaxKey) { throw new ArgumentOutOfRangeException("TypeId must be between 0 and MaxKey!"); }
        }
    }

    public interface IObjectPool<T> {
        T GetObject();
        bool PoolObject(T objectToDeactive);
    }

    public class SimpleObjectPool<T> : IObjectPool<T> {
        public T GetObject() {
            throw new NotImplementedException();
        }

        public bool PoolObject(T objectToDeactive) {
            throw new NotImplementedException();
        }
    }
}

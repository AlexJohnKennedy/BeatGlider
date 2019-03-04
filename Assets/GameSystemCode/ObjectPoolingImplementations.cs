using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackSystem;

namespace ObjectPoolingImplementations {
    public class SimpleCategoricalObjectPool<T> : ICategoricalObjectPool<T> {
        private IObjectPool<T>[] subPools;

        public SimpleCategoricalObjectPool(int numKeys, IObjectPool<T>[] subPools) {
            if (subPools.Length != numKeys) { throw new ArgumentException("SimpleCategoricalObjectPool was passed an incorrect number of subPools; must match maxKey!"); }
            NumKeys = numKeys;
            this.subPools = subPools;
        }

        public int NumKeys { get; }

        public T GetObject(int typeId) {
            CheckTypeId(typeId);
            return subPools[typeId].GetObject();
        }

        public bool PoolObject(T objectToDeactivate, int typeId) {
            CheckTypeId(typeId);
            return subPools[typeId].PoolObject(objectToDeactivate);
        }

        private void CheckTypeId(int t) {
            if (t < 0 || t >= NumKeys) { throw new ArgumentOutOfRangeException("TypeId must be between 0 and MaxKey!"); }
        }
    }

    public interface IObjectPool<T> {
        T GetObject();
        bool PoolObject(T objectToDeactive);
    }

    public class QueueBasedObjectPool<T> : IObjectPool<T> {

        // This function delegate will return us the correct 'archetype' of whatever object we are pooling! 
        // It will create new objects for us if we need it.
        private readonly Func<T> objectFactoryFunction;  
        private Queue<T> pool;

        // We have to option to pre-pool (pre instantiate) object instances rather than making them on demand, by passing in
        // a factory function, which this class can call successively to pre-create object instances and place them in the pool.
        public QueueBasedObjectPool(Func<T> objectFactoryFunction, int numObjectsToPreinitialise) {
            pool = new Queue<T>(numObjectsToPreinitialise + 5);     // Five spares? I don't know why i chose this randomly.

            // Pre-initialise the pool!
            for (int i=0; i < numObjectsToPreinitialise; i++) {
                pool.Enqueue(objectFactoryFunction());
            }
        }

        public T GetObject() {
            // We must instantiate an object if our pool is currently empty! This is not really desireable, since instantiations are a performance hit.
            // But at least we won't have to garbage collect it!
            if (pool.Count == 0) {
                return objectFactoryFunction();
            }
            else {
                return pool.Dequeue();
            }
        }

        public bool PoolObject(T objectToDeactive) {
            pool.Enqueue(objectToDeactive);
            return true;
        }
    }
}

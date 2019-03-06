using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackSystem;

namespace ObjectPoolingImplementations {

    public class SharableSubpoolCategoricalObjectPool<T> : ICategoricalObjectPool<T> {
        private IObjectPool<T>[] subPools;
        private IDictionary<int, int> keyMap;

        public SharableSubpoolCategoricalObjectPool(IDictionary<int, int> keyMap, IObjectPool<T>[] subPools) {
            // Make sure the size of the subpools array matches the distinct number of values in the map.
            // Make sure the max value in the map is not outside the range of the subpools array
            // Make sure there are no negative keys.
            // If all three are met, then the output values of the map will be [0, 1, 2, ... n] where n is the last valid index into the subPools array
            if (keyMap.Values.Distinct().Count() != subPools.Length || keyMap.Values.Max() >= subPools.Length || keyMap.Values.Min() < 0) {
                throw new ArgumentException("Malformed subPools or keyMap objects (or both) passed to a SharableSubpoolCategoricalObjectPool constructor");
            }
            this.keyMap = keyMap;
            this.subPools = subPools;
            NumKeys = subPools.Length;
        }

        public SharableSubpoolCategoricalObjectPool(HashSet<int>[] inputValueSets, IObjectPool<T>[] subPools) {
            if (inputValueSets.Length != subPools.Length) { throw new ArgumentException("SharableSubpoolCategoricalObjectPool was passed bad arguments in constructor"); }
            keyMap = new Dictionary<int, int>();
            for (int i=0; i < inputValueSets.Length; i++) {
                if (inputValueSets[i].Count == 0 || inputValueSets[i].Count >= subPools.Length) {
                    throw new ArgumentException("SharableSubpoolCategoricalObjectPool was passed bad arguments in constructor");
                }
                foreach (int input in inputValueSets[i]) {
                    keyMap.Add(input, i);
                }
            }
            this.subPools = subPools;
            NumKeys = subPools.Length;
        }

        public int NumKeys { get; }

        public T GetObject(int typeId) {
            CheckTypeId(typeId);
            return subPools[keyMap[typeId]].GetObject();
        }

        public bool PoolObject(T objectToDeactivate, int typeId) {
            CheckTypeId(typeId);
            return subPools[keyMap[typeId]].PoolObject(objectToDeactivate);
        }

        private void CheckTypeId(int t) {
            if (t < 0 || t >= NumKeys) { throw new ArgumentOutOfRangeException("TypeId must be between 0 and MaxKey!"); }
        }
    }

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

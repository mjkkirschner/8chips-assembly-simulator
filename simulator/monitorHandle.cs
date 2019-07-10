using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;

namespace simulator
{
    public class MonitorHandle<T>
    {
        public T Address { get; }
        private ObservableCollection<T> Memory;
        private List<T> historicalValues = new List<T>();
        public MonitorHandle(T address, ObservableCollection<T> memory)
        {
            this.Address = address;
            this.Memory = memory;
            var addressAsInt32 = Convert.ToInt32(address);
            this.historicalValues.Add(this.Memory[addressAsInt32]);
            //hookup our monitor
            this.Memory.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
                {
                    if (e.OldStartingIndex == addressAsInt32)
                    {
                        this.historicalValues.Add((T)Convert.ChangeType(e.NewItems[0], typeof(T)));
                    }
                }
            };
        }
        public List<T> getValues()
        {
            return this.historicalValues;
        }
    }
}
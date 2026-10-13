using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CFIT.AppFramework.ResourceStores
{
    public class SimStore(SimConnectManager manager)
    {
        protected virtual SimConnectManager SimConnect { get; } = manager;
        protected virtual ConcurrentDictionary<string, ISimResourceSubscription> SimResources { get; } = [];
        protected virtual ConcurrentDictionary<string, int> RefCount { get; } = [];
        protected virtual int Count { get { return SimResources.Count; } }

        public virtual ISimResourceSubscription this[string name]
        {
            get
            {
                if (TryGet(name, out ISimResourceSubscription subscription))
                    return subscription;
                else
                    return null;
            }
        }

        public virtual ISimResourceSubscription AddVariable(string name, string unit = SimUnitType.Number)
        {
            return Add(name, () => { return SimConnect.VariableManager.Subscribe(name, unit); });
        }

        public virtual ISimResourceSubscription AddEvent(string name)
        {
            return Add(name, () => { return SimConnect.EventManager.Subscribe(name); });
        }

        public virtual ISimResourceSubscription AddState(string name)
        {
            return Add(name, () => { return SimConnect.StateManager.Subscribe(name); });
        }

        public virtual ISimResourceSubscription AddInput(string name)
        {
            return Add(name, () => { return SimConnect.InputManager.Subscribe(name); });
        }

        protected virtual ISimResourceSubscription Add(string name, Func<ISimResourceSubscription> subscribeFunc)
        {
            if (!SimResources.TryGetValue(name, out ISimResourceSubscription subscription))
            {
                subscription = subscribeFunc?.Invoke();
                SimResources.Add(name, subscription);
                RefCount.Add(name, 1);
            }
            else
                RefCount[name] = RefCount[name] + 1;

            return subscription;
        }

        public virtual ISimResourceSubscription Remove(string name)
        {
            if (SimResources.TryGetValue(name, out ISimResourceSubscription subscription))
            {
                if (RefCount.ContainsKey(name))
                {
                    if (RefCount[name] <= 1)
                    {
                        subscription?.Unsubscribe();
                        SimResources.Remove(name);
                        RefCount.Remove(name);
                    }
                    else
                        RefCount[name] = RefCount[name] - 1;
                }
                else
                {
                    Logger.Warning($"The Resource '{name}' has no Ref Count");
                    subscription?.Unsubscribe();
                    SimResources.Remove(name);
                }
            }

            return subscription;
        }

        public virtual void Clear()
        {
            foreach (var subscription in SimResources.Values)
                subscription?.Unsubscribe();
            SimResources.Clear();
        }

        public virtual bool Contains(string name)
        {
            return SimResources.ContainsKey(name);
        }

        public virtual bool TryGet(string name, out ISimResourceSubscription subscription)
        {
            return SimResources.TryGetValue(name, out subscription);
        }

        public virtual bool RegisterEventHandler(string name, Func<ISimResourceSubscription, object, Task> eventHandler)
        {
            if (TryGet(name, out ISimResourceSubscription subscription))
            {
                subscription.OnReceived += eventHandler;
                return true;
            }

            return false;
        }

        public virtual bool UnregisterEventHandler(string name, Func<ISimResourceSubscription, object, Task> eventHandler)
        {
            if (TryGet(name, out ISimResourceSubscription subscription))
            {
                subscription.OnReceived -= eventHandler;
                return true;
            }

            return false;
        }

        public virtual async Task<double> WaitValueAsync(string name, double value)
        {
            if (TryGet(name, out ISimResourceSubscription subscription))
            {
                var waiter = new SimResourceWaiter(subscription, SimConnect.Token);
                value = await waiter.WaitValueAsync(value);
                waiter.Dispose();
                return value;
            }
            else
                return 0;
        }
    }
}

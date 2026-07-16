using System;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    [Serializable]
    public class StringEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public class IntEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public class FloatEvent : UnityEvent<float>
    {
    }

    [Serializable]
    public class BoolEvent : UnityEvent<bool>
    {
    }

    [Serializable]
    public class BankCustomerEvent : UnityEvent<BankCustomer>
    {
    }
}

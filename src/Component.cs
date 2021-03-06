﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YewLib
{
    public interface IUpdatable
    {
        void Update();
    }
    
    public class Component : IUpdatable
    {
        public string ClassName { get; set; }
        
        public Node Node { get; set; }
        public Component(string className = null)
        {
            ClassName = className;
        }
        
        public virtual void ReceiveProps(View view)
        {
        }

        private Dictionary<string, List<IState>> states;
        private Dictionary<string, int> stateCounters;

        protected State<T> UseState<T>(Func<T> initialValue,
            [System.Runtime.CompilerServices.CallerMemberName]
            string callerName = "")
        {
            return UseState(callerName, initialValue);
        }
        
        protected State<T> UseState<T>(T initialValue = default,
            [System.Runtime.CompilerServices.CallerMemberName]
            string callerName = "")
        {
            return UseState(callerName, () => initialValue);
        }
        
        public State<T> UseAtom<T>(string key, Func<T> initialValue)
        {
            var state = Yew.UseAtom(key, initialValue);
            state.Subscribers.Add(this);
            return state;
        }
        
        public State<T> UseAtom<T>(string key, T initialValue = default)
        {
            var state = Yew.UseAtom(key, () => initialValue);
            state.Subscribers.Add(this);
            return state;
        }
        
        public T UseAtomValue<T>(string key)
        {
            var state = Yew.UseAtom<T>(key);
            if (state == null) return default(T);
            state.Subscribers.Add(this);
            return state.Value;
        }
        
        protected State<T> UseState<T>(string key, Func<T> initialValue)
        {
            State<T> state;
            if (states == null)
            {
                states = new Dictionary<string, List<IState>>();
                stateCounters = new Dictionary<string, int>();
            }

            if (!states.ContainsKey(key))
            {
                states[key] = new List<IState>();
                stateCounters[key] = 0;
            }

            var stateCounter = stateCounters[key];
            var maybeState = states[key].Count > stateCounter ? states[key][stateCounter] : null;
            if (maybeState is State<T> rightState)
            {
                state = rightState;
            }
            else
            {
                state = new State<T>() {Value = initialValue()};
                state.Subscribers.Add(this);
                states[key].Add(state);
            }
            stateCounters[key]++;
            return state;
        }

        public void PrepareRender()
        {
            stateCounters = stateCounters?.ToDictionary(i => i.Key, _ => 0);
        }
        
        public virtual View Render()
        {
            return null;
        }

        public void Update()
        {
            if (Runtime.HasInstance)
                Runtime.Instance.QueueForUpdate(Node);
            else
                Node.Update();
        }
        
        public static Button Button(string label, Action onClick, string className = null)
        {
            return new Button(label, onClick, className);
        }
            
        public static Label Label(string label, string className = null)
        {
            return new Label(label, className);
        }

        public void RequestAnimationFrame(Action raf)
        {
            if (Runtime.Instance == null)
            {
                Debug.Log("You must install the Yew Runtime before you can use animations.");
                return;
            }
            Runtime.Instance.RequestAnimationFrame(raf);
        }

        public void UseRaf(Func<bool> raf)
        {
            if (Runtime.Instance == null)
            {
                Debug.Log("You must install the Yew Runtime before you can use animations.");
                return;
            }
            Runtime.Instance.RequestAnimationFrame(() =>
            {
                if (raf()) UseRaf(raf);
            });
        }

        private HashSet<IEnumerator> coroutines = new HashSet<IEnumerator>();
        public void UseCoroutine(Func<IEnumerator> coroutine)
        {
            if (Runtime.Instance == null)
            {
                Debug.Log("You must install the Yew Runtime before you can use animations.");
                return;
            }
            var c = UseState(coroutine);
            if (coroutines.Contains(c.Value)) return;
            coroutines.Add(c.Value);
            Runtime.Instance.StartCoroutine(c.Value);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that can animate float parameters on renderers
    /// </summary>
    public class MaterialFloatAnimator : MonoBehaviour
    {
        public bool ResetOnEnable = true;

        /// <summary>
        /// Animated float parameters
        /// </summary>
        public Parameter[] Params;
        /// <summary>
        /// Common targets. Use it if your parameters are not use any target renderers
        /// </summary>
        public Renderer[] Targets;
        MaterialPropertyBlock[] blocks;

        /// <summary>
        /// Property Blocks of targets
        /// </summary>
        public MaterialPropertyBlock[] Blocks
        {
            get
            {
                if (blocks == null)
                {
                    // there we are get every property block for every renderer
                    blocks = new MaterialPropertyBlock[Targets.Length];
                    for (int i = 0; i < Targets.Length; i++)
                    {
                        blocks[i] = new MaterialPropertyBlock();
                        Targets[i].GetPropertyBlock(blocks[i]);
                    }
                }
                return blocks;
            }
        }

        private void Update()
        {
            // main parameters animation cycle
            foreach (Parameter p in Params)
            {
                // animate only if parameter is enabled
                if (p.Enabled)
                {
                    // there we choose
                    //      that we use per parameter target renderers (if any exists)
                    // OR
                    //      that we use common target renderers
                    if (p.Targets == null || p.Targets.Length == 0)
                    {
                        // there we update and push property block to renderer target if parameter checked as Instanced
                        if (p.Instanced)
                        {
                            for (int i = 0; i < Targets.Length; i++)
                            {
                                Blocks[i].SetFloat(p.PropertyName, p.CurrentValue);
                                Targets[i].SetPropertyBlock(Blocks[i]);
                            }
                        }
                        else // not instanced, using material instance copy via Renderer.material property (be careful that call this property clone shared materials)
                        {
                            foreach (Renderer t in Targets)
                            {
                                foreach (Material mat in t.materials)
                                {
                                    if (mat.HasProperty(p.PropertyName))
                                    {
                                        mat.SetFloat(p.PropertyName, p.CurrentValue);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // same as previous but use parameter own target renderers
                        if (p.Instanced)
                        {
                            for (int i = 0; i < p.Targets.Length; i++)
                            {
                                p.Blocks[i].SetFloat(p.PropertyName, p.CurrentValue);
                                Targets[i].SetPropertyBlock(p.Blocks[i]);
                            }
                        }
                        else
                        {
                            foreach (Renderer t in p.Targets)
                            {
                                foreach (Material mat in t.materials)
                                {
                                    if (mat.HasProperty(p.PropertyName))
                                    {
                                        mat.SetFloat(p.PropertyName, p.CurrentValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get animated float parameter data by id (name)
        /// </summary>
        /// <param name="id">parameter name</param>
        /// <returns>animated float parameter</returns>
        public Parameter GetParameter(string id)
        {
            foreach (Parameter p in Params)
            {
                if (p.ID.Equals(id))
                {
                    return p;
                }
            }

            throw new NotExistParameter("There is no parameter with id " + id);
        }

        /// <summary>
        /// Try get parameter
        /// </summary>
        /// <param name="id">parameter name</param>
        /// <param name="parameter">out parameter</param>
        /// <returns>has or no</returns>
        public bool TryGetParameter(string id, out Parameter parameter)
        {
            parameter = null;
            foreach (Parameter p in Params)
            {
                if (p.ID.Equals(id))
                {
                    parameter = p;
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            // there we setup StatTime value to current time, for properly working of enabled parameters
            // for example if parameter will be enabled and you instantiate object so animation will be played at spawn
            foreach (Parameter p in Params)
            {
                if (p.Enabled)
                {
                    p.StartTime = Time.time;
                }
            }
        }

        private void OnEnable()
        {
            if (ResetOnEnable)
            {
                foreach (var p in Params)
                {
                    if (p.Enabled)
                    {
                        p.StartTime = Time.time;
                    }
                }
                Update();
            }
        }

        /// <summary>
        /// Start animation of float parameter by id (name)
        /// </summary>
        /// <param name="id">parameter name</param>
        public void StartAnimation(string id)
        {
            // set that parameter is enabled and set time to current
            Parameter p;
            if (TryGetParameter(id, out p))
            {
                p.Enabled = true;
                p.StartTime = Time.time;
            }
        }

        /// <summary>
        /// Stop animation of float parameter by id (name)
        /// </summary>
        /// <param name="id">parameter name</param>
        public void StopAnimation(string id)
        {
            // just set enabled value of parameter to false to stop animation
            Parameter p;
            if (TryGetParameter(id, out p))
            {
                p.Enabled = false;
            }
        }

        /// <summary>
        /// Animated float parameter class
        /// </summary>
        [System.Serializable]
        public class Parameter
        {
            /// <summary>
            /// Parameter ID (name)
            /// </summary>
            public string ID;
            /// <summary>
            /// Is animation enabled
            /// </summary>
            public bool Enabled;
            /// <summary>
            /// Shader float property name
            /// </summary>
            public string PropertyName;
            /// <summary>
            /// Start interpolation value
            /// </summary>
            public float StartValue;
            /// <summary>
            /// End interpolation value
            /// </summary>
            public float TargetValue;
            /// <summary>
            /// Interpolation curve
            /// </summary>
            public AnimationCurve Curve;
            /// <summary>
            /// Animation duration
            /// </summary>
            public float Duration;
            /// <summary>
            /// Timecoded events
            /// </summary>
            public ParameterEvent[] Events;
            /// <summary>
            /// Renderer targets
            /// </summary>
            public Renderer[] Targets;
            /// <summary>
            /// Is property instanced
            /// </summary>
            public bool Instanced = false;
            MaterialPropertyBlock[] blocks;

            /// <summary>
            /// Start animation time
            /// </summary>
            [HideInInspector]
            public float StartTime;

            /// <summary>
            /// Material property blocks of targets renderers
            /// </summary>
            public MaterialPropertyBlock[] Blocks
            {
                get
                {
                    // there we are get every property block for every renderer
                    if (blocks == null)
                    {
                        blocks = new MaterialPropertyBlock[Targets.Length];
                        for (int i = 0; i < Targets.Length; i++)
                        {
                            blocks[i] = new MaterialPropertyBlock();
                            Targets[i].GetPropertyBlock(blocks[i]);
                        }
                    }
                    return blocks;
                }
            }

            /// <summary>
            /// Interpolated value
            /// </summary>
            public float CurrentValue
            {
                get
                {
                    // return final value, because we can't divide by zero
                    if (Duration == 0)
                    {
                        return TargetValue;
                    }

                    // calculate elapsed time from animation begin
                    float elapsedTime = (Time.time - StartTime);

                    // invoke all events which:
                    //      not invoked in past
                    //      timecode is timecode is smaller than elapsed time
                    // also there we reset invoked flags if event timecode is greater than elapsed time
                    for (int i = 0; i < Events.Length; i++)
                    {
                        if (Events[i].Pushed && elapsedTime < Events[i].Timecode)
                        {
                            Events[i].Pushed = false;
                        }
                        else if (!Events[i].Pushed && elapsedTime >= Events[i].Timecode)
                        {
                            Events[i].Event.Invoke();
                            Events[i].Pushed = true;
                        }
                    }

                    // interpolate value and return it
                    return Mathf.Lerp(StartValue, TargetValue, Curve.Evaluate(elapsedTime / Duration));
                }
            }

            /// <summary>
            /// Get timecode event by event name
            /// </summary>
            /// <param name="eventName">event name</param>
            /// <returns>timecode event</returns>
            public UnityEvent GetEvent(string eventName)
            {
                for (int i = 0; i < Events.Length; i++)
                {
                    if (Events[i].EventName.Equals(eventName))
                        return Events[i].Event;
                }

                throw new NotExistEventInParameter("There is no event with name " + eventName);
            }
        }

        /// <summary>
        /// Timecode event class
        /// </summary>
        [System.Serializable]
        public class ParameterEvent
        {
            /// <summary>
            /// Event name
            /// </summary>
            public string EventName = "NewEvent";
            /// <summary>
            /// Timecode in seconds
            /// </summary>
            public float Timecode;
            /// <summary>
            /// Invokable event
            /// </summary>
            public UnityEvent Event;
            /// <summary>
            /// Is event already invoked
            /// </summary>
            [HideInInspector]
            public bool Pushed;
        }

        public class NotExistEventInParameter : Exception
        {
            public NotExistEventInParameter(string message) : base(message)
            {
            }
        }

        public class NotExistParameter : Exception
        {
            public NotExistParameter(string message) : base(message)
            {
            }
        }
    }
}
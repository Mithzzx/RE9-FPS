using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Behavior.SerializationExample
{
    public class SerializationExampleSceneController : MonoBehaviour
    {
        private class GameObjectResolver : RuntimeSerializationUtility.IUnityObjectResolver<string>
        {
            public string Map(UnityEngine.Object obj) => obj ? obj.name : null;

            public TSerializedType Resolve<TSerializedType>(string mappedValue) where TSerializedType : Object
            {
                // It would be recommended to have a more robust way to resolve objects by name or id using a registry.
                GameObject obj = GameObject.Find(mappedValue);
                if (!obj)
                {
                    // If we didn't find the object by name in the scene, it might be a prefab.
                    GameObject[] prefabs = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var prefab in prefabs)
                    {
                        if (prefab.name == mappedValue)
                        {
                            return prefab as TSerializedType;
                        }
                    }

                    return null;
                }
                if (typeof(TSerializedType) == typeof(GameObject))
                {
                    return obj as TSerializedType;
                }
                if (typeof(Component).IsAssignableFrom(typeof(TSerializedType)))
                {
                    return obj.GetComponent<TSerializedType>();
                }
                return null;
            }
        }

        [SerializeField] private GameObject m_agentPrefab;
        [SerializeField] private int m_count;

        private List<GameObject> m_agents = new();
        private GameObjectResolver m_GameObjectResolver = new();
        private RuntimeSerializationUtility.JsonBehaviorSerializer m_JsonSerializer = new();

        private Dictionary<GameObject, Vector3> m_agentPositions = new();
        private Dictionary<GameObject, string> m_serializedAgents = new();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            Random.InitState(0);
            for (int idx = 0; idx < m_count; ++idx)
            {
                GameObject agent = Instantiate(m_agentPrefab, transform);
                agent.name = $"Agent_{idx}";
                m_agents.Add(agent);
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(5, 5, 150, 90), "Menu");
            if (GUI.Button(new Rect(10, 30, 130, 20), "Save"))
            {
                SerializeAgents();
            }
            if (GUI.Button(new Rect(10, 60, 130, 20), "Load"))
            {
                DeserializeAgents();
            }
        }

        private void SerializeAgents()
        {
            m_serializedAgents.Clear();
            m_agentPositions.Clear();

            foreach (var agent in m_agents)
            {
                string data = agent.GetComponent<BehaviorGraphAgent>().Serialize(m_JsonSerializer, m_GameObjectResolver);
                m_serializedAgents.Add(agent, data);
                m_agentPositions.Add(agent, agent.transform.position);
            }
        }

        private void DeserializeAgents()
        {
            foreach (var agent in m_agents)
            {
                if (m_serializedAgents.TryGetValue(agent, out var data))
                {
                    agent.GetComponent<BehaviorGraphAgent>().Deserialize(data, m_JsonSerializer, m_GameObjectResolver);
                    agent.transform.position = m_agentPositions[agent];
                }
            }
        }
    }
}
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Object = UnityEngine.Object;

public class Pool
{
    private Transform parentPool;

    private Dictionary<int, Stack<GameObject>> cachedObjects = new Dictionary<int, Stack<GameObject>>();

    private Dictionary<int, int> cachedIds = new Dictionary<int, int>();
    //protected int index;


    public Pool PopulateWith(GameObject prefab, int amount, int amountPerTick, int tickSize = 1)
    {
        var key = prefab.GetInstanceID();
        var stack = new Stack<GameObject>(amount);
        cachedObjects.Add(key, stack);
        Observable.IntervalFrame(tickSize, FrameCountType.EndOfFrame).Where(val => amount > 0).Subscribe(_loop =>
        {
            Observable.Range(0, amountPerTick).Where(check => amount > 0).Subscribe(_pop =>
            {
                //index = amount;
                var go = Populate(prefab, Vector3.zero, Quaternion.identity, parentPool);
                go.SetActive(false);
                cachedIds.Add(go.GetInstanceID(), key);
                cachedObjects[key].Push(go);
                amount--;
                if (amount == 0) Debug.Log("DONE!");
            });
        });
        return this;
    }

    public void SetParent(Transform parent)
    {
        parentPool = parent;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position = default(Vector3),
        Quaternion rotation = default(Quaternion),
        Transform parent = null, bool localPos = false) //Активация объекта
    {
        //index++;
        var key = prefab.GetInstanceID();
        Stack<GameObject> stack;

        var stacked = cachedObjects.TryGetValue(key, out stack);
        if (stacked && stack.Count > 0)
        {
            var transform = stack.Pop().transform;
            transform.SetParent(parent);
            transform.rotation = rotation;
            transform.gameObject.SetActive(true);
            if (!localPos) transform.position = position;
            else transform.localPosition = position;
            IPoolable poolable = transform.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawn(); //Не работает при включении игры
            }

            return transform.gameObject;
        }

        if (!stacked) cachedObjects.Add(key, new Stack<GameObject>());
        Debug.LogWarning(prefab + " Догрузка");
        var createdPrefab = Populate(prefab, position, rotation, parent, localPos);
        cachedIds.Add(createdPrefab.GetInstanceID(), key);

        return createdPrefab;
    }

    public void Despawn(GameObject go)
    {
        //index--;
        go.SetActive(false);
        cachedObjects[cachedIds[go.GetInstanceID()]].Push(go);
        var poolable = go.GetComponent<IPoolable>();
        if (poolable != null) poolable.OnDespawn();
        if (parentPool != null) go.transform.SetParent(parentPool);
    }

    public void Dispose()
    {
        parentPool = null;
        cachedObjects.Clear();
        cachedIds.Clear();
    }

    GameObject Populate(GameObject prefab, Vector3 position = default(Vector3),
        Quaternion rotation = default(Quaternion), Transform parent = null, bool localPos = false)
    {
        var go = Object.Instantiate(prefab, position, rotation, parent).transform;
        //go.name += "_" + index; 
        if (!localPos) go.position = position;
        else go.localPosition = position;
        // Моё внедрение ///////////////// 
        /*IPoolable poolable = go.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }
        */
        ////////////////////////
        return go.gameObject;
    }
}
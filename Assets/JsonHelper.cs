using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJsonArray<T>(string json)
    {
        string wrappedJson = $"{{\"items\":{json}}}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
        return wrapper.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}

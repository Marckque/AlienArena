using UnityEngine;

public static class ExtensionMethods
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    // In this particular case, we do not want to modify the Y component of any entity 
    // Nonetheless, the function is still returning a Vector3 to avoid to use "Y" as "Z", if we had a Vector2.
    public static Vector3 RandomVector3()
    {
        float randomX = Random.Range(-1f, 1f);
        //float randomY = Random.Range(-1f, 1f);
        float randomZ = Random.Range(-1f, 1f);

        return new Vector3(randomX, 0f, randomZ).normalized; // Again, "0f" in "Y" should be randomY.
    }
}
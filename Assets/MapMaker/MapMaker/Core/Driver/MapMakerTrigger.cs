using UnityEngine;

namespace MapMaker.Core.Driver
{
    public sealed class MapMakerTrigger : MonoBehaviour
    {
        public MapMakerDriver Driver;

        public void Run()
        {
            Debug.Log("Trigger Run called");
            Driver?.Run();
        }
    }  
}

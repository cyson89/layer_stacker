using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerConnector : MonoBehaviour
{
    public GameObject connectedLayer;

    private void OnDestroy()
    {
        Destroy(connectedLayer);
    }


}

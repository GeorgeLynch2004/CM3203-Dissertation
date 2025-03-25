using UnityEngine;

public class OpacityController : MonoBehaviour
{
    public Transform player; // Reference to the player object
    public float maxDistance = 3f; // Maximum distance for full opacity
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color[] originalColors;

    void Start()
    {
        player = SessionManager.Instance.XROrigin.transform;
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        float alpha = Mathf.Clamp01(distance / maxDistance);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                Color newColor = originalColors[i];
                newColor.a = alpha;
                renderers[i].material.color = newColor;
            }
            else
            {
                renderers[i].enabled = distance > maxDistance;
            }
        }
    }
}

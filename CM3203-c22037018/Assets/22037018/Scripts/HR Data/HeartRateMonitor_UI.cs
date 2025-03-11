using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartRateMonitor_UI : MonoBehaviour
{
    public bool connected = false;
    public HeartRateMonitor connector;
    [SerializeField] private DataManager dataManager;
    [SerializeField] private string infoText;

    // Default values for heart rate monitor - can be changed in inspector
    public string device_name = "Polar H10 D7E96D26";
    public string service_id = "{0000180d-0000-1000-8000-00805f9b34fb}"; // Standard Heart Rate Service UUID
    public string read_characteristic = "{00002a37-0000-1000-8000-00805f9b34fb}"; // Heart Rate Measurement characteristic

    private void Start()
    {
        dataManager = FindObjectOfType<DataManager>();
    }

    public void BeginConnection()
    {
        connector = new HeartRateMonitor(this, this);
        connect();
    }

    public void connect()
    {
        if (device_name.Length > 0 && service_id.Length > 0 && read_characteristic.Length > 0)
        {
            StartCoroutine(connector.connect(device_name, service_id, read_characteristic));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (connected)
        {

            connector.Update();
            infoText = connector.output;

            if (connector.has_heartRate)
            {

            }
        }

    }

    private void OnApplicationQuit()
    {
        connector.quit();
    }

    // Methods to update device parameters from UI
    public void change_device_name(string _device_name)
    {
        device_name = _device_name;
    }

    public void change_service_id(string _service_id)
    {
        service_id = _service_id;
    }

    public void change_read_characteristic(string _read_characteristic)
    {
        read_characteristic = _read_characteristic;
    }

    // Method to reconnect with new parameters
    public void reconnect()
    {
        connected = false;
        connector.quit();
        connect();
    }


}
// FTMS_UI.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FTMS_UI : MonoBehaviour
{

    public static FTMS_UI Instance {  get; private set; }

    // Start is called before the first frame update
    public bool connected = false;
    public FTMS_IndoorBike connector;
    public string info;
    public string hrm_info; // Additional infoText field for heart rate monitor
    public Text resistance_show;

    public string device_name = "WattbikePT28004316";
    public string service_id = "{babf1723-cedb-444c-88c3-c672c7a59806}";
    public string read_characteristic = "{babf1724-cedb-444c-88c3-c672c7a59806}";
    public string write_characteristic = "{babf1725-cedb-444c-88c3-c672c7a59806}";
    public string hrm_device_name = "Polar H10 D7E96D26";
    public string hrm_service_id = "{0000180d-0000-1000-8000-00805f9b34fb}";
    public string hrm_read_characteristic = "{00002a37-0000-1000-8000-00805f9b34fb}";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
            connector = new FTMS_IndoorBike(this);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        
    }

    public void connect()
    {
        if (device_name.Length > 0 && service_id.Length > 0 && read_characteristic.Length > 0 && write_characteristic.Length > 0)
        {
            StartCoroutine(connector.connect(device_name, service_id, read_characteristic, write_characteristic, hrm_device_name, hrm_service_id, hrm_read_characteristic));
            connected = true;
        }
    }

    public void write_resistance(float val)
    {
        if (connected)
        {
            connector.write_resistance(val);
            resistance_show.text = "Resistance: " + Mathf.FloorToInt(val).ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (connected)
        {
            connector.Update();
            info = connector.output;
            hrm_info = connector.hrm_output; // Update HRM info
        }
    }

    private void OnApplicationQuit()
    {
        connector.quit();
    }

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

    public void change_write_characteristic(string _write_characteristic)
    {
        write_characteristic = _write_characteristic;
    }
}
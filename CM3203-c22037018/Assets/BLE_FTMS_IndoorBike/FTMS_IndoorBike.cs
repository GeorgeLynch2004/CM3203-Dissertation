using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;
using SysDebug = System.Diagnostics.Debug;

public class FTMS_IndoorBike
{
    string device_name;
    string service_id;
    string read_characteristic;
    string write_characteristic;

    string hrm_device_name;
    string hrm_service_id;
    string hrm_read_characteristic;

    public bool want_connect = true;
    Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    string selectedDeviceId = "";
    string selectedServiceId = "";
    string selectedCharacteristicId = "";

    string selectedHRMDeviceId = "";
    string selectedHRMServiceId = "";
    string selectedHRMCharacteristicId = "";

    bool isSubscribed = false;
    bool isHRMSubscribed = false;

    public string output;
    public string hrm_output;
    public float speed; public bool has_speed = false;
    public float average_speed; public bool has_average_speed = false;
    public float rpm; public bool has_rpm = false;
    public float average_rpm; public bool has_average_rpm = false;
    public float distance; public bool has_distance = false;
    public float resistance; public bool has_resistance = false;
    public float power; public bool has_power = false;
    public float average_power; public bool has_average_power = false;
    public float expended_energy; public bool has_expended_energy = false;


    public int heartRate; public bool has_heartRate = false;
    public bool contact_detected; public bool has_contact = false;
    public bool rr_interval_present; public bool has_rr_interval = false;
    public List<int> rr_intervals = new List<int>(); // RR intervals in ms

    string lastError;

    float last_write_time = 0.0f;

    int sended_resistance = 0;

    MonoBehaviour mono;
    public FTMS_IndoorBike(MonoBehaviour _mono)
    {
        mono = _mono;
    }

    public IEnumerator connect(string _device_name = "WattbikePT28004316", string _service_id = "{babf1723-cedb-444c-88c3-c672c7a59806}", string _read_characteristic = "{babf1724-cedb-444c-88c3-c672c7a59806}", string _write_characteristic = "{babf1725-cedb-444c-88c3-c672c7a59806}", string _hrm_device_name = "HeartRateMonitor", string _hrm_service_id = "{hrm_service_id}", string _hrm_read_characteristic = "{hrm_read_characteristic}")
    {
        if (!want_connect) yield break;

        device_name = _device_name;
        service_id = _service_id;
        read_characteristic = _read_characteristic;
        write_characteristic = _write_characteristic;

        hrm_device_name = _hrm_device_name;
        hrm_service_id = _hrm_service_id;
        hrm_read_characteristic = _hrm_read_characteristic;

        quit();

        // Connect to HRM
        yield return mono.StartCoroutine(connect_hrm_device());
        if (selectedHRMDeviceId.Length == 0) yield break;

        Debug.Log("connecting HRM device finish");

        yield return mono.StartCoroutine(connect_hrm_service());
        if (selectedHRMServiceId.Length == 0) yield break;

        Debug.Log("connecting HRM service finish");

        yield return mono.StartCoroutine(connect_hrm_read_characteristic());
        if (selectedHRMCharacteristicId.Length == 0) yield break;

        Debug.Log("connecting HRM read characteristic finish");

        read_hrm_subscribe();

        yield return mono.StartCoroutine(connect_device());
        if (selectedDeviceId.Length == 0) yield break;

        yield return mono.StartCoroutine(connect_service());
        if (selectedDeviceId.Length == 0) yield break;

        yield return mono.StartCoroutine(connect_read_characteristic());
        if (selectedDeviceId.Length == 0) yield break;

        read_subscribe();

    }

    IEnumerator connect_device()
    {
        Debug.Log("connecting device...");
        BleApi.StartDeviceScan();
        BleApi.ScanStatus status = BleApi.ScanStatus.AVAILABLE;
        BleApi.DeviceUpdate device_res = new BleApi.DeviceUpdate();
        do
        {
            status = BleApi.PollDevice(ref device_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (!devices.ContainsKey(device_res.id))
                    devices[device_res.id] = new Dictionary<string, string>() {
                        { "name", "" },
                        { "isConnectable", "False" }
                    };
                if (device_res.nameUpdated)
                    devices[device_res.id]["name"] = device_res.name;
                if (device_res.isConnectableUpdated)
                    devices[device_res.id]["isConnectable"] = device_res.isConnectable.ToString();
                // consider only devices which have a name and which are connectable
                if (devices[device_res.id]["name"] == device_name && devices[device_res.id]["isConnectable"] == "True")
                {
                    selectedDeviceId = device_res.id;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedDeviceId.Length == 0)
                {
                    Debug.LogError("device " + device_name + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_service()
    {
        Debug.Log("connecting service...");
        BleApi.ScanServices(selectedDeviceId);
        BleApi.ScanStatus status;
        BleApi.Service service_res = new BleApi.Service();
        do
        {
            status = BleApi.PollService(out service_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (service_res.uuid == service_id)
                {
                    selectedServiceId = service_res.uuid;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedServiceId.Length == 0)
                {
                    Debug.LogError("service " + service_id + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_read_characteristic()
    {
        Debug.Log("connecting characteristic...");
        BleApi.ScanCharacteristics(selectedDeviceId, selectedServiceId);
        BleApi.ScanStatus status;
        BleApi.Characteristic characteristics_res = new BleApi.Characteristic();
        do
        {
            status = BleApi.PollCharacteristic(out characteristics_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (characteristics_res.uuid == read_characteristic)
                {
                    selectedCharacteristicId = characteristics_res.uuid;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedCharacteristicId.Length == 0)
                {
                    Debug.LogError("characteristic " + read_characteristic + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_hrm_device()
    {
        Debug.Log("connecting HRM device...");
        BleApi.StartDeviceScan();
        BleApi.ScanStatus status = BleApi.ScanStatus.AVAILABLE;
        BleApi.DeviceUpdate device_res = new BleApi.DeviceUpdate();
        do
        {
            status = BleApi.PollDevice(ref device_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (!devices.ContainsKey(device_res.id))
                    devices[device_res.id] = new Dictionary<string, string>() {
                        { "name", "" },
                        { "isConnectable", "False" }
                    };
                if (device_res.nameUpdated)
                    devices[device_res.id]["name"] = device_res.name;
                if (device_res.isConnectableUpdated)
                    devices[device_res.id]["isConnectable"] = device_res.isConnectable.ToString();
                // consider only devices which have a name and which are connectable
                if (devices[device_res.id]["name"] == hrm_device_name && devices[device_res.id]["isConnectable"] == "True")
                {
                    selectedHRMDeviceId = device_res.id;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedHRMDeviceId.Length == 0)
                {
                    Debug.LogError("HRM device " + hrm_device_name + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_hrm_service()
    {
        Debug.Log("connecting HRM service...");
        BleApi.ScanServices(selectedHRMDeviceId);
        BleApi.ScanStatus status;
        BleApi.Service service_res = new BleApi.Service();
        do
        {
            status = BleApi.PollService(out service_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (service_res.uuid == hrm_service_id)
                {
                    selectedHRMServiceId = service_res.uuid;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedHRMServiceId.Length == 0)
                {
                    Debug.LogError("HRM service " + hrm_service_id + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_hrm_read_characteristic()
    {
        Debug.Log("connecting HRM characteristic...");
        BleApi.ScanCharacteristics(selectedHRMDeviceId, selectedHRMServiceId);
        BleApi.ScanStatus status;
        BleApi.Characteristic characteristics_res = new BleApi.Characteristic();
        do
        {
            status = BleApi.PollCharacteristic(out characteristics_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (characteristics_res.uuid == hrm_read_characteristic)
                {
                    selectedHRMCharacteristicId = characteristics_res.uuid;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedHRMCharacteristicId.Length == 0)
                {
                    Debug.LogError("HRM characteristic " + hrm_read_characteristic + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    void read_subscribe()
    {
        Debug.Log("Subscribe...");
        BleApi.SubscribeCharacteristic_Read(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        isSubscribed = true;
    }

    void read_hrm_subscribe()
    {
        Debug.Log("Subscribe HRM...");
        BleApi.SubscribeCharacteristic_Read(selectedHRMDeviceId, selectedHRMServiceId, selectedHRMCharacteristicId, false);
        isHRMSubscribed = true;
    }

    public void quit()
    {
        BleApi.Quit();
    }

    public void Update()
    {
        if (isSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData
            {
                buf = new byte[512] // Ensure the buffer is initialized
            };

            while (BleApi.PollData(out res, false))
            {
                if (res.deviceId != selectedDeviceId) return;

                has_speed = false;
                has_average_speed = false;
                has_rpm = false;
                has_average_rpm = false;
                has_distance = false;
                has_resistance = false;
                has_power = false;
                has_average_power = false;
                has_expended_energy = false;

                output = String.Empty;
                int index = 0;
                int flags = BitConverter.ToUInt16(res.buf, index);
                index += 2;
                if ((flags & 0) == 0)
                {
                    has_speed = true;
                    float value = (float)BitConverter.ToUInt16(res.buf, index);
                    speed = (value * 1.0f) / 100.0f;
                    output += "Speed: " + speed + "\\n";
                    index += 2;
                }
                if ((flags & 2) > 0)
                {
                    has_average_speed = true;
                    average_speed = BitConverter.ToUInt16(res.buf, index);
                    output += "Average Speed: " + average_speed + "\\n";
                    index += 2;
                }
                if ((flags & 4) > 0)
                {
                    rpm = (BitConverter.ToUInt16(res.buf, index) * 1.0f) / 2.0f;
                    output += "RPM: (rev/min): " + rpm + "\\n";
                    index += 2;
                }
                if ((flags & 8) > 0)
                {
                    average_rpm = (BitConverter.ToUInt16(res.buf, index) * 1.0f) / 2.0f;
                    output += "Average RPM: " + average_rpm + "\\n";
                    index += 2;
                }
                if ((flags & 16) > 0)
                {
                    distance = BitConverter.ToUInt16(res.buf, index); // ????
                    output += "Distance (meter): " + distance + "\\n";
                    index += 2;
                }
                if ((flags & 32) > 0)
                {
                    resistance = BitConverter.ToInt16(res.buf, index);
                    output += "Resistance: " + resistance + "\\n";
                    index += 2;
                }
                if ((flags & 64) > 0)
                {
                    power = BitConverter.ToInt16(res.buf, index);
                    output += "Power (Watt): " + power + "\\n";
                    index += 2;
                }
                if ((flags & 128) > 0)
                {
                    average_power = BitConverter.ToInt16(res.buf, index);
                    output += "AveragePower: " + average_power + "\\n";
                    index += 2;
                }
                if ((flags & 256) > 0)
                {
                    expended_energy = BitConverter.ToUInt16(res.buf, index);
                    output += "ExpendedEnergy: " + expended_energy + "\\n";
                    index += 2;
                }
            }

            // log potential errors
            BleApi.ErrorMessage res_err = new BleApi.ErrorMessage();
            BleApi.GetError(out res_err);
            if (lastError != res_err.msg)
            {
                Debug.LogError(res_err.msg);
                lastError = res_err.msg;
            }
        }

        if (isHRMSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData
            {
                buf = new byte[512] // Ensure the buffer is initialized
            };

            while (BleApi.PollData(out res, false))
            {
                if (res.deviceId != selectedHRMDeviceId) return;

                has_heartRate = false;
                has_contact = false;
                has_rr_interval = false;
                rr_intervals.Clear();

                hrm_output = String.Empty;
                int index = 0;

                // Read first byte for flags
                byte flags = res.buf[index++];

                // Heart Rate Value Format bit (0 = UINT8, 1 = UINT16)
                bool isUint16 = (flags & 0x01) != 0;

                // Sensor Contact Status bits
                bool contactSensorPresent = (flags & 0x04) != 0;
                if (contactSensorPresent)
                {
                    has_contact = true;
                    contact_detected = (flags & 0x02) != 0;
                    hrm_output += "Contact: " + (contact_detected ? "Detected" : "Not Detected") + "\n";
                }

                // Energy Expended Status bit
                bool energyExpendedPresent = (flags & 0x08) != 0;

                // RR-Interval bit
                rr_interval_present = (flags & 0x10) != 0;
                has_rr_interval = rr_interval_present;

                // Read the Heart Rate Measurement Value
                if (isUint16)
                {
                    heartRate = BitConverter.ToUInt16(res.buf, index);
                    index += 2;
                }
                else
                {
                    heartRate = res.buf[index++];
                }
                has_heartRate = true;
                hrm_output += "Heart Rate: " + heartRate + " bpm\n";

                // Skip Energy Expended field if present
                if (energyExpendedPresent)
                {
                    index += 2; // UINT16
                }

                // Read RR-Intervals if present
                if (rr_interval_present)
                {
                    // Calculate how many RR intervals are in the packet
                    // Each RR interval is 2 bytes (UINT16)
                    int remainingBytes = res.size - index;
                    int rrIntervalCount = remainingBytes / 2;

                    hrm_output += "RR Intervals: ";
                    for (int i = 0; i < rrIntervalCount; i++)
                    {
                        int rrInterval = BitConverter.ToUInt16(res.buf, index);
                        index += 2;
                        // RR intervals are in 1/1024 second units, convert to milliseconds
                        int rrIntervalMs = (int)Math.Round(rrInterval * 1000.0 / 1024.0);
                        rr_intervals.Add(rrIntervalMs);
                        hrm_output += rrIntervalMs + "ms ";
                    }
                    hrm_output += "\n";
                }
            }

            // log potential errors
            BleApi.ErrorMessage res_err = new BleApi.ErrorMessage();
            BleApi.GetError(out res_err);
            if (lastError != res_err.msg)
            {
                Debug.LogError(res_err.msg);
                lastError = res_err.msg;
            }
        }
    }

    private byte[] Convert16(string strText)
    {
        strText = strText.Replace(" ", "");
        byte[] bText = new byte[strText.Length / 2];
        for (int i = 0; i < strText.Length / 2; i++)
        {
            bText[i] = Convert.ToByte(Convert.ToInt32(strText.Substring(i * 2, 2), 16));
        }
        return bText;
    }

    public void Write(string msg)
    {
        byte[] payload22 = Convert16(msg);
        BleApi.BLEData data = new BleApi.BLEData
        {
            buf = new byte[512],
            size = (short)payload22.Length,
            deviceId = selectedDeviceId,
            serviceUuid = selectedServiceId,
            characteristicUuid = write_characteristic
        };

        Array.Copy(payload22, data.buf, payload22.Length);
        BleApi.SendData(in data, false);
    }

    public void write_resistance(float val)
    {
        write_resistance(Mathf.FloorToInt(val));
    }

    public void write_resistance(int val)
    {
        if (Time.time - last_write_time < 0.1f)
        {
            return;
        }
        if (sended_resistance == val)
        {
            Debug.Log("Resistance value is the same as before; no need to resend.");
            return;
        }

        last_write_time = Time.time;
        sended_resistance = val;

        Debug.Log("write resistance: " + val);

        BleApi.SubscribeCharacteristic_Write(selectedDeviceId, selectedServiceId, write_characteristic, false);
        Write("00");
        byte resistance1 = Convert.ToByte(val % 256);
        byte resistance2 = Convert.ToByte(val / 256);
        byte[] payload = { 0x11, 0x00, 0x00, resistance1, resistance2, 0x00, 0x00 };
        BleApi.BLEData data = new BleApi.BLEData
        {
            buf = new byte[512],
            deviceId = selectedDeviceId,
            serviceUuid = selectedServiceId,
            characteristicUuid = write_characteristic
        };

        Array.Copy(payload, data.buf, payload.Length);
        data.size = (short)payload.Length;
        BleApi.SendData(in data, false);
    }
}
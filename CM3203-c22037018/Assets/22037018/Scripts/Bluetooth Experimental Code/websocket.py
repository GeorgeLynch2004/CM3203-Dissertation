import asyncio
import websockets
import json
from bleak import BleakClient, BleakScanner, BleakError

# Device Details
HR_DEVICE_NAME = "Polar H10 D7E96D26"
HR_SERVICE_UUID = "0000180d-0000-1000-8000-00805f9b34fb"
HR_CHARACTERISTIC_UUID = "00002a37-0000-1000-8000-00805f9b34fb"

EB_DEVICE_NAME = "WattbikePT28004316"
EB_SERVICE_UUID = "00001826-0000-1000-8000-00805f9b34fb"  # FTMS Service UUID
EB_MEASUREMENT_CHARACTERISTIC_UUID = "00002ad2-0000-1000-8000-00805f9b34fb"  # FTMS Measurement Characteristic UUID

# WebSocket Settings
WS_PORT = 8765
WS_SERVER = f"ws://localhost:{WS_PORT}"

# Global WebSocket connection variable
active_websocket = None

# Global variables for storing sensor data
heart_rate = None
speed = None
average_speed = None
rpm = None
average_rpm = None
distance = None
resistance = None
power = None
average_power = None
expended_energy = None

last_output_time = 0

async def find_device(device_name):
    """Scans and returns the Bluetooth address of the device by name."""
    print("Scanning for devices...")
    devices = await BleakScanner.discover()
    for device in devices:
        if device.name and device_name in device.name:
            print(f"Found {device_name} at {device.address}")
            return device.address
    print(f"Device {device_name} not found. Ensure it is on and nearby.")
    return None

async def heart_rate_callback(_, data: bytearray):
    """Callback function to handle heart rate data from BLE."""
    global heart_rate
    heart_rate = data[1]  # Extract heart rate value
    await output_data()

async def exercise_bike_callback(_, data: bytearray):
    """Callback function to handle exercise bike FTMS data from BLE."""
    global speed, average_speed, rpm, average_rpm, distance, resistance, power, average_power, expended_energy

    index = 0
    flags = int.from_bytes(data[index:index+2], byteorder='little')
    index += 2

    if (flags & 0) == 0:
        speed = int.from_bytes(data[index:index+2], byteorder='little') / 100.0
        index += 2

    if (flags & 2) > 0:
        average_speed = int.from_bytes(data[index:index+2], byteorder='little')
        index += 2

    if (flags & 4) > 0:
        rpm = int.from_bytes(data[index:index+2], byteorder='little') / 2.0
        index += 2

    if (flags & 8) > 0:
        average_rpm = int.from_bytes(data[index:index+2], byteorder='little') / 2.0
        index += 2

    if (flags & 16) > 0:
        distance = int.from_bytes(data[index:index+2], byteorder='little')
        index += 2

    if (flags & 32) > 0:
        resistance = int.from_bytes(data[index:index+2], byteorder='little', signed=True)
        index += 2

    if (flags & 64) > 0:
        power = int.from_bytes(data[index:index+2], byteorder='little', signed=True)
        index += 2

    if (flags & 128) > 0:
        average_power = int.from_bytes(data[index:index+2], byteorder='little', signed=True)
        index += 2

    if (flags & 256) > 0:
        expended_energy = int.from_bytes(data[index:index+2], byteorder='little')
        index += 2

    await output_data()

async def output_data():
    """Outputs the current sensor data."""
    global last_output_time
    current_time = asyncio.get_event_loop().time()

    if current_time - last_output_time >= 1:
        # Create a dictionary with default values for null fields
        message_data = {
            "heart_rate": heart_rate if heart_rate is not None else 0,
            "speed": speed if speed is not None else 0,
            "average_speed": average_speed if average_speed is not None else 0,
            "rpm": rpm if rpm is not None else 0,
            "average_rpm": average_rpm if average_rpm is not None else 0,
            "distance": distance if distance is not None else 0,
            "resistance": resistance if resistance is not None else 0,
            "power": power if power is not None else 0,
            "average_power": average_power if average_power is not None else 0,
            "expended_energy": expended_energy if expended_energy is not None else 0
        }
        
        message = json.dumps(message_data)
        print(f"------------------------\nSpeed: {message_data['speed']}\nRPM: {message_data['rpm']}\nPower: {message_data['power']}")

        # Send data to Unity via WebSocket
        if active_websocket:
            try:
                await active_websocket.send(message)
            except Exception as e:
                print(f"WebSocket Send Error: {e}")

        last_output_time = current_time

async def connect_to_device(device_name, service_uuid, read_characteristic, callback):
    """Connects to a BLE device and subscribes to notifications."""
    device_address = await find_device(device_name)
    if not device_address:
        return

    while True:
        try:
            async with BleakClient(device_address) as client:
                print(f"Connected to {device_name} ({device_address})")

                if client.is_connected:  # Access is_connected as a property
                    print(f"Successfully connected to {device_name}")
                    await client.start_notify(read_characteristic, callback)
                    print(f"Notifications started for {device_name}")

                    # Keep the connection alive
                    try:
                        while True:
                            await asyncio.sleep(1)
                    except asyncio.CancelledError:
                        await client.stop_notify(read_characteristic)
                        print(f"Disconnected from {device_name}.")
                        break
        except (BleakError, OSError) as e:
            print(f"Connection error with {device_name}: {e}")
            await asyncio.sleep(5)  # Wait before attempting to reconnect

# Update the websocket_server function to match the current websockets library API
async def websocket_server(websocket):
    """Handles WebSocket connections."""
    global active_websocket
    active_websocket = websocket
    print("WebSocket Connected")

    try:
        await asyncio.Future()  # Keep connection open
    except Exception as e:
        print(f"WebSocket Error: {e}")
    finally:
        active_websocket = None  # Reset connection on disconnect

async def main():
    """Runs the BLE and WebSocket servers concurrently."""
    server = await websockets.serve(websocket_server, "0.0.0.0", WS_PORT)
    print(f"WebSocket Server running on {WS_SERVER}")

    await asyncio.gather(
        connect_to_device(EB_DEVICE_NAME, EB_SERVICE_UUID, EB_MEASUREMENT_CHARACTERISTIC_UUID, exercise_bike_callback),
        server.wait_closed()
    )

if __name__ == "__main__":
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)
    try:
        loop.run_until_complete(main())
    except KeyboardInterrupt:
        print("Script terminated by user.")
    finally:
        for task in asyncio.all_tasks(loop):
            task.cancel()
        loop.run_until_complete(asyncio.gather(*asyncio.all_tasks(loop), return_exceptions=True))
        loop.close()
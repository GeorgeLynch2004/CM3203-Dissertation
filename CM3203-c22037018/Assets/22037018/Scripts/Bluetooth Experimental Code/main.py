import asyncio
import socket
from bleak import BleakScanner, BleakClient

# Replace these values with your sensor's details
DEVICE_NAME = "Your_HeartRate_Device_Name"
SERVICE_UUID = "your-service-uuid"
CHARACTERISTIC_UUID = "your-characteristic-uuid"

# UDP Configuration (Unity)
UDP_IP = "127.0.0.1"  # Change if Unity is running on a different machine
UDP_PORT = 5052
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)


async def find_device():
    """Scan for the heart rate sensor using the device name."""
    print("Scanning for Bluetooth devices...")
    devices = await BleakScanner.discover()
    for device in devices:
        if device.name and DEVICE_NAME in device.name:
            print(f"Found device: {device.name} - {device.address}")
            return device.address  # Return the MAC address of the device
    print("Device not found. Make sure it's powered on and in range.")
    return None


def parse_heart_rate(data):
    """Extract heart rate from raw BLE data."""
    if len(data) > 1:
        return data[1]  # Heart rate is usually in the second byte
    return 0


async def notification_handler(sender, data):
    """Handles incoming heart rate data and sends it to Unity via UDP."""
    heart_rate = parse_heart_rate(data)
    message = str(heart_rate).encode('utf-8')
    sock.sendto(message, (UDP_IP, UDP_PORT))
    print(f"Sent heart rate: {heart_rate}")


async def main():
    device_address = await find_device()
    if not device_address:
        return  # Stop if the device isn't found

    async with BleakClient(device_address) as client:
        if await client.is_connected():
            print(f"Connected to {DEVICE_NAME}")
            await client.start_notify(CHARACTERISTIC_UUID, notification_handler)
            await asyncio.Future()  # Keep running indefinitely


if __name__ == "__main__":
    asyncio.run(main())

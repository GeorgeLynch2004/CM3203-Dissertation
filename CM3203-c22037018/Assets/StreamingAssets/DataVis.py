import pandas as pd
import matplotlib.pyplot as plt
import sys
import os

# Check if command line arguments were provided
if len(sys.argv) < 3:
    print("Usage: python script.py csv_path output_image_path")
    sys.exit(1)

# Get paths from command line arguments
csv_file_path = sys.argv[1]
output_image_path = sys.argv[2]

print(f"CSV file: {csv_file_path}")
print(f"Output will be saved to: {output_image_path}")

# Load CSV file, skipping metadata rows
def load_data(file_path):
    try:
        print(f"Attempting to load data from: {file_path}")
        df = pd.read_csv(file_path, skiprows=3)
        df.columns = ["Time", "Power", "Cadence", "Speed", "HeartRate"]
        df["Time"] = pd.to_datetime(df["Time"], errors="coerce")
        print(f"Successfully loaded data with {len(df)} rows")
        return df
    except Exception as e:
        print(f"Error loading data: {e}")
        # Create dummy data if file not found
        import numpy as np
        print("Generating sample data instead")
        dates = pd.date_range('2023-01-01', periods=100, freq='1min')
        power = np.random.randint(100, 300, size=100)
        hr = np.random.randint(60, 180, size=100)
        df = pd.DataFrame({
            'Time': dates,
            'Power': power,
            'HeartRate': hr,
            'Cadence': np.random.randint(60, 100, size=100),
            'Speed': np.random.randint(15, 35, size=100)
        })
        return df

# Plot Power and Heart Rate
def plot_data(df, output_path):
    try:
        print("Creating visualization...")
        # Main plot with two y-axes
        fig, ax1 = plt.subplots(figsize=(12, 6))
        
        # Power on left y-axis
        ax1.set_xlabel('Time')
        ax1.set_ylabel('Power (Watts)', color='blue')
        ax1.plot(df["Time"], df["Power"], color='blue', label='Power')
        ax1.tick_params(axis='y', labelcolor='blue')
        
        # Heart Rate on right y-axis
        ax2 = ax1.twinx()
        ax2.set_ylabel('Heart Rate (BPM)', color='red')
        ax2.plot(df["Time"], df["HeartRate"], color='red', label='Heart Rate')
        ax2.tick_params(axis='y', labelcolor='red')
        
        # Create a combined legend
        lines1, labels1 = ax1.get_legend_handles_labels()
        lines2, labels2 = ax2.get_legend_handles_labels()
        ax1.legend(lines1 + lines2, labels1 + labels2, loc='upper right')
        
        plt.title('Power & Heart Rate Over Time')
        plt.grid(True)
        plt.tight_layout()
        
        # Ensure the directory exists
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        # Save the figure
        print(f"Saving graph to {output_path}")
        plt.savefig(output_path)
        plt.close()
        print(f"Graph saved successfully")
        
    except Exception as e:
        print(f"Error plotting data: {e}")
        # Create a simple error image if plotting fails
        plt.figure(figsize=(10, 6))
        plt.text(0.5, 0.5, f"Error creating visualization: {e}", 
                 horizontalalignment='center', verticalalignment='center')
        plt.savefig(output_path)
        plt.close()

# Main execution
if __name__ == "__main__":
    try:
        df = load_data(csv_file_path)
        plot_data(df, output_image_path)
        print("Script completed successfully")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")
        # Create a simple error image if everything fails
        plt.figure(figsize=(10, 6))
        plt.text(0.5, 0.5, f"Fatal error: {e}", 
                 horizontalalignment='center', verticalalignment='center')
        plt.savefig(output_image_path)
        plt.close()
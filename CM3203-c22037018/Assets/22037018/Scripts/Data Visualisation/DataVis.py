import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import sys
import os

# Check if command line arguments were provided
if len(sys.argv) < 3:
    print("Usage: python script.py csv_path output_dir")
    sys.exit(1)

# Get paths from command line arguments
csv_file_path = sys.argv[1]
output_dir = sys.argv[2]

print(f"CSV file: {csv_file_path}")
print(f"Output directory: {output_dir}")

# Load CSV file, skipping metadata rows
def load_data(file_path):
    try:
        print(f"Attempting to load data from: {file_path}")
        df = pd.read_csv(file_path, skiprows=3)
        df.columns = ["Time", "Power", "Cadence", "Speed", "HeartRate"]
        df["Time"] = pd.to_datetime(df["Time"], errors="coerce")
        df = df.dropna(subset=["Time"])  # Drop rows with invalid timestamps
        df["ElapsedSeconds"] = (df["Time"] - df["Time"].iloc[0]).dt.total_seconds()
        print(f"Successfully loaded data with {len(df)} rows")
        return df
    except Exception as e:
        print(f"Error loading data: {e}")
        sys.exit(1)

# Function to plot individual graphs with line of best fit
def plot_graph(x, y, xlabel, ylabel, title, color, output_path, y_min=0, y_max=None):
    plt.figure(figsize=(12, 6))
    plt.plot(x, y, color=color, label=title)
    
    # Compute and plot line of best fit
    coef = np.polyfit(x, y, 1)
    poly1d_fn = np.poly1d(coef)
    plt.plot(x, poly1d_fn(x), '--', color='black', label='Trendline')
    
    plt.xlabel(xlabel)
    plt.ylabel(ylabel)
    plt.ylim(y_min, y_max)
    plt.title(title)
    plt.legend()
    plt.grid(True)
    plt.tight_layout()
    plt.savefig(output_path)
    plt.close()
    print(f"Saved: {output_path}")

# Function to plot combined Power & Heart Rate graph
def plot_combined(df, output_path):
    fig, ax1 = plt.subplots(figsize=(12, 6))
    ax1.set_xlabel("Elapsed Time (seconds)")
    ax1.set_ylabel("Power (Watts)", color='blue')
    ax1.plot(df["ElapsedSeconds"], df["Power"], color='blue', label='Power')
    ax1.tick_params(axis='y', labelcolor='blue')
    ax1.set_ylim(0, df["Power"].max())
    
    ax2 = ax1.twinx()
    ax2.set_ylabel("Heart Rate (BPM)", color='red')
    ax2.plot(df["ElapsedSeconds"], df["HeartRate"], color='red', label='Heart Rate')
    ax2.tick_params(axis='y', labelcolor='red')
    ax2.set_ylim(0, None)
    
    fig.tight_layout()
    plt.title("Power & Heart Rate Over Time")
    plt.savefig(output_path)
    plt.close()
    print(f"Saved: {output_path}")

# Main execution
if __name__ == "__main__":
    df = load_data(csv_file_path)
    os.makedirs(output_dir, exist_ok=True)
    
    plot_graph(df["ElapsedSeconds"], 
               df["Power"], "Elapsed Time (seconds)", "Power (Watts)", "Power Over Time", "blue", 
               os.path.join(output_dir, "power_graph.png"), 0, df["Power"].max())
    plot_graph(df["ElapsedSeconds"], 
               df["HeartRate"], "Elapsed Time (seconds)", "Heart Rate (BPM)", "Heart Rate Over Time", "red", 
               os.path.join(output_dir, "heartrate_graph.png"), 0, None)
    plot_combined(df, os.path.join(output_dir, "combined_graph.png"))
    
    print("Script completed successfully")

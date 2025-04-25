#region Imports

import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors
from matplotlib.patches import Polygon
import matplotlib.cm as cm
import seaborn as sns
import numpy as np
import os
import sys
from scipy.stats import f_oneway, ttest_rel, wilcoxon
from statsmodels.stats.multicomp import pairwise_tukeyhsd
from statsmodels.formula.api import ols
import statsmodels.api as sm
import re
import warnings


#endregion

#region Loaders

def load_performance_data(file_path):
    try:
        print(f"Attempting to load data from: {file_path}")
        df = pd.read_csv(file_path, skiprows=3)
        df.columns = ["Time", "Power", "Cadence", "Speed", "HeartRate"]
        df["Time"] = pd.to_datetime(df["Time"], errors="coerce")
        df = df.dropna(subset=["Time"])
        df["ElapsedSeconds"] = (df["Time"] - df["Time"].iloc[0]).dt.total_seconds()
        print(f"Successfully loaded data with {len(df)} rows")
        return df
    except Exception as e:
        print(f"Error loading data: {e}")
        sys.exit(1)

def load_rpe_scale_data(file_path):
    try:
        print(f"Attempting to load RPE data from: {file_path}")
        df = pd.read_csv(file_path)

        expected_cols = {
            'Participant Number': 'ParticipantID',
            'What scenario did you just complete?': 'Scenario',
            'Choose the number that best describes your level of exertion.': 'RPE'
        }

        if not set(expected_cols.keys()).issubset(df.columns):
            raise ValueError("Input file must contain: 'Participant Number', 'What scenario...', and 'Choose the number...' columns.")

        # Rename and select necessary columns
        df = df[list(expected_cols.keys())].rename(columns=expected_cols)

        # Extract only the numeric part from RPE using regex
        df["RPE"] = df["RPE"].astype(str).str.extract(r'(\d+)')
        df["RPE"] = pd.to_numeric(df["RPE"], errors="coerce")

        # Drop incomplete rows
        df = df.dropna(subset=["ParticipantID", "Scenario", "RPE"])

        # Find participants who completed all 3 scenarios
        counts = df.groupby("ParticipantID")["Scenario"].nunique()
        complete_ids = counts[counts == 3].index

        # Filter the dataframe
        df = df[df["ParticipantID"].isin(complete_ids)]

        print(f"Filtered to {len(complete_ids)} participants with all three scenarios.")
        print(f"Final dataset includes {len(df)} valid RPE entries.")
        return df

    except Exception as e:
        print(f"[ERROR] Failed to load RPE data: {e}")
        sys.exit(1)

def load_imi_data(file_path):
    """
    Load and preprocess the IMI data from a CSV file.
    
    Parameters:
    -----------
    file_path : str
        Path to the IMI data CSV file
        
    Returns:
    --------
    pandas.DataFrame
        Processed IMI data
    """
    try:
        print(f"Attempting to load IMI data from: {file_path}")
        df = pd.read_csv(file_path)

        # Column mapping for renaming
        expected_cols = {
            'Participant ID:': 'ParticipantID',
            'What scenario did you complete?': 'Scenario',
            'I enjoyed engaging in my session very much.': 'Enjoyment1',
            'After my session, I feel motivated to reach my goals.': 'Motivation',
            'I put a lot of effort into my session.': 'Effort1',
            'Engaging in my session was enjoyable.': 'Enjoyment2',
            'I believe that the method I used to track my workout could be of value to my exercise goals.': 'Value',
            'I believe I am pretty good at working out.': 'Competence',
            'I tried very hard during my exercises.': 'Effort2',
            'I found my session to be interesting.': 'Interest',
            'I engaged in my workouts because I wanted to.': 'Choice',
            'I had fun during my session.': 'Fun',
            'I felt very tense during my session.': 'Pressure1',
            'I experienced a lot of pressure during my session.': 'Pressure2'
        }

        # Check if required columns exist
        if not set(expected_cols.keys()).issubset(df.columns):
            missing_cols = set(expected_cols.keys()) - set(df.columns)
            raise ValueError(f"Input file missing required columns: {missing_cols}")

        # Select and rename only the columns we need
        df = df[list(expected_cols.keys())].rename(columns=expected_cols)
        
        # Convert ratings to numeric values
        for col in df.columns:
            if col not in ['ParticipantID', 'Scenario']:
                # Extract numeric part using regex for values like "7: Very True"
                df[col] = df[col].astype(str).str.extract(r'(\d+)')
                # Convert to numeric
                df[col] = pd.to_numeric(df[col], errors="coerce")
        
        # Drop rows with missing essential data
        df = df.dropna(subset=["ParticipantID", "Scenario"])
        
        # Find participants who completed all 3 scenarios
        scenario_counts = df.groupby("ParticipantID")["Scenario"].nunique()
        complete_ids = scenario_counts[scenario_counts == 3].index
        
        # Filter to only include participants who completed all scenarios
        df = df[df["ParticipantID"].isin(complete_ids)]
        
        print(f"Filtered to {len(complete_ids)} participants with all three scenarios.")
        print(f"Final dataset includes {len(df)} valid IMI entries.")
        
        return df

    except Exception as e:
        print(f"[ERROR] Failed to load IMI data: {e}")
        return None
    

#endregion

#region Time Series Analysis

def plot_moving_average(all_data, metric, horizontalLinesFlag, verticalLinesFlag, output_dir, window=30):
    plt.figure(figsize=(12, 6))

    color_map = {
        "Baseline": "green",
        "Cooperative": "blue",
        "Competitive": "red"
    }

    for label, df in all_data.items():
        df_sorted = df.sort_values("ElapsedSeconds")
        ma = df_sorted[metric].rolling(window=window, min_periods=1).mean()
        x = df_sorted["ElapsedSeconds"]
        color = color_map.get(label, "gray")

        plt.plot(x, ma, label=f"{label} MA", color=color)

        if verticalLinesFlag and len(x) > 0:
            plt.axvline(x.iloc[-1], color=color, linestyle='--', alpha=0.6)

        if horizontalLinesFlag and len(ma) > 0:
            plt.axhline(ma.iloc[-1], color=color, linestyle='--', alpha=0.6)

    plt.title(f"{metric} - Moving Average Trend")
    plt.xlabel("Elapsed Time (s)")
    plt.ylabel(metric)
    plt.legend()
    plt.grid(True)
    plt.tight_layout()
    save_path = os.path.join(output_dir, f'{metric}_MA.png')
    plt.savefig(save_path)
    plt.close()
    print(f"Saved moving average plot: {save_path}")

#endregion

#region ANOVA and Stats Tests

def run_anova(all_data, metric, output_dir):
    data = []
    for label, df in all_data.items():
        if df.empty or metric not in df.columns:
            print(f"[WARNING] Data for '{label}' is empty or missing '{metric}' column. Skipping.")
            continue
        valid_values = df[metric].dropna()
        if valid_values.empty:
            print(f"[WARNING] No valid '{metric}' values in '{label}'. Skipping.")
            continue
        for value in valid_values:
            data.append({"Condition": label, metric: value})

    data_df = pd.DataFrame(data)

    if data_df.empty:
        print(f"[WARNING] No valid data available for ANOVA on metric '{metric}'. Skipping.")
        return

    if data_df["Condition"].nunique() < 2:
        print(f"[WARNING] Not enough conditions with data to perform ANOVA for '{metric}'. Skipping.")
        return

    try:
        model = ols(f'{metric} ~ C(Condition)', data=data_df).fit()
        anova = sm.stats.anova_lm(model, typ=2)
        print(f"\nANOVA Results for {metric}:\n", anova)

        tukey = pairwise_tukeyhsd(data_df[metric], data_df['Condition'])

        # Save results
        output_file = os.path.join(output_dir, f"{metric}_ANOVA_Results.txt")
        with open(output_file, "w") as f:
            f.write(f"ANOVA Results for {metric}:\n\n")
            f.write(str(anova))
            f.write("\n\nTukey HSD Results:\n")
            f.write(str(tukey))

        print(f"Saved ANOVA + Tukey results to: {output_file}")

    except Exception as e:
        print(f"[ERROR] Failed to run ANOVA for '{metric}': {e}")

#endregion

#region Rate of Perceived Exertion

def analyze_rpe_differences(csv_path):
    """
    Analyze RPE data by calculating average scores per scenario and the percentage differences
    between scenarios.
    
    Parameters:
    -----------
    csv_path : str
        Path to the RPE data CSV file
        
    Returns:
    --------
    tuple
        (average_rpe_dict, percentage_diff_dict)
    """
    try:
        # Load the RPE data
        df = load_rpe_scale_data(csv_path)
        
        # Calculate average RPE for each scenario
        avg_rpe = df.groupby('Scenario')['RPE'].mean().to_dict()
        print("\nAverage RPE Scores:")
        for scenario, score in avg_rpe.items():
            print(f"{scenario}: {score:.2f}")
        
        # Calculate percentage differences between scenarios
        scenarios = ["Baseline", "Cooperative", "Competitive"]
        percent_diff = {}
        
        for i, scenario1 in enumerate(scenarios):
            for scenario2 in scenarios[i+1:]:
                # Calculate how much greater scenario2 is compared to scenario1
                if scenario1 in avg_rpe and scenario2 in avg_rpe:
                    diff_key = f"{scenario2} vs {scenario1}"
                    percent_diff[diff_key] = ((avg_rpe[scenario2] - avg_rpe[scenario1]) / avg_rpe[scenario1]) * 100
                    
                    # Calculate how much greater scenario1 is compared to scenario2
                    reverse_key = f"{scenario1} vs {scenario2}"
                    percent_diff[reverse_key] = ((avg_rpe[scenario1] - avg_rpe[scenario2]) / avg_rpe[scenario2]) * 100
        
        print("\nPercentage Differences:")
        for comparison, diff in percent_diff.items():
            direction = "higher" if diff > 0 else "lower"
            print(f"{comparison}: {abs(diff):.2f}% {direction}")
            
        return avg_rpe, percent_diff
        
    except Exception as e:
        print(f"[ERROR] Failed to analyze RPE differences: {e}")
        return {}, {}

def generate_rpe_boxplot(csv_path, output_path="RPE_Boxplot.png"):
    try:
        # Load data
        df = load_rpe_scale_data(csv_path)
        base_order = ["Baseline", "Cooperative", "Competitive"]

        counts = df["Scenario"].value_counts().to_dict()
        
        # Create ordered labels like "Baseline (n=12)"
        label_map = {s: f"{s} (n={counts.get(s, 0)})" for s in base_order}
        df["ScenarioLabel"] = df["Scenario"].map(label_map)

        # Convert to ordered categorical for plotting
        df["ScenarioLabel"] = pd.Categorical(df["ScenarioLabel"],
                                             categories=[label_map[s] for s in base_order],
                                             ordered=True)

        # Define the color map for the boxplots
        color_map = {
            label_map["Baseline"]: "green",  # Uses actual count
            label_map["Cooperative"]: "blue",
            label_map["Competitive"]: "red"
        }

        # Plot
        plt.figure(figsize=(8, 5))
        sns.boxplot(data=df, x="ScenarioLabel", y="RPE", hue="ScenarioLabel", 
                    palette=color_map, legend=False)  # Use hue and palette for color mapping
        
        plt.title("Rate of Perceived Exertion (RPE) Across Scenarios")
        plt.xlabel("Scenario")
        plt.ylabel("RPE Score")
        plt.grid(True)
        plt.tight_layout()

        # Save
        plt.savefig(output_path)
        plt.close()
        print(f"Saved RPE boxplot to: {output_path}")

    except Exception as e:
        print(f"[ERROR] Failed to generate RPE boxplot: {e}")

#endregion

#region IMI Analysis

# Define IMI subscales
IMI_SUBSCALES = {
    "Interest/Enjoyment": ["Enjoyment1", "Enjoyment2", "Interest", "Fun"],
    "Perceived Competence": ["Competence"],
    "Effort": ["Effort1", "Effort2"],
    "Value/Usefulness": ["Value"],
    "Pressure/Tension": ["Pressure1", "Pressure2"],
    "Perceived Choice": ["Choice"],
    "Motivation": ["Motivation"]
}

def calculate_composite_scores(df):
    
    result_df = df.copy()
    
    for subscale, columns in IMI_SUBSCALES.items():
        if len(columns) > 1:
            result_df[subscale] = result_df[columns].mean(axis=1)
        else:
            result_df[subscale] = result_df[columns[0]]
    
    return result_df

def generate_subscale_boxplots(df, output_dir="output"):
    
    try:
        os.makedirs(output_dir, exist_ok=True)
        base_order = ["Baseline", "Cooperative", "Competitive"]
        
        # Create scenario labels with counts
        counts = df["Scenario"].value_counts().to_dict()
        label_map = {s: f"{s} (n={counts.get(s, 0)})" for s in base_order}
        df["ScenarioLabel"] = df["Scenario"].map(label_map)
        
        # Convert to ordered categorical for plotting
        df["ScenarioLabel"] = pd.Categorical(df["ScenarioLabel"],
                                         categories=[label_map[s] for s in base_order],
                                         ordered=True)
        
        # Define the color map
        color_map = {
            label_map["Baseline"]: "green",
            label_map["Cooperative"]: "blue",
            label_map["Competitive"]: "red"
        }
        
        # Generate individual boxplots for each original item
        all_items = [col for col in df.columns if col not in ['ParticipantID', 'Scenario', 'ScenarioLabel'] + list(IMI_SUBSCALES.keys())]
        
        for item in all_items:
            plt.figure(figsize=(8, 5))
            sns.boxplot(data=df, x="ScenarioLabel", y=item, hue="ScenarioLabel", 
                        palette=color_map, legend=False)
            
            plt.title(f"{item} Scores Across Scenarios")
            plt.xlabel("Scenario")
            plt.ylabel(f"{item} Score (1-7)")
            plt.ylim(0.5, 7.5)  # Set y-axis for 1-7 scale
            plt.grid(True)
            plt.tight_layout()
            
            output_path = os.path.join(output_dir, f"{item}_Boxplot.png")
            plt.savefig(output_path)
            plt.close()
            print(f"Saved {item} boxplot to: {output_path}")
        
        # Generate boxplots for composite subscales
        for subscale in IMI_SUBSCALES.keys():
            plt.figure(figsize=(8, 5))
            sns.boxplot(data=df, x="ScenarioLabel", y=subscale, hue="ScenarioLabel", 
                        palette=color_map, legend=False)
            
            plt.title(f"{subscale} Scores Across Scenarios")
            plt.xlabel("Scenario")
            plt.ylabel(f"{subscale} Score (1-7)")
            plt.ylim(0.5, 7.5)  # Set y-axis for 1-7 scale
            plt.grid(True)
            plt.tight_layout()
            
            output_path = os.path.join(output_dir, f"{subscale}_Composite_Boxplot.png")
            plt.savefig(output_path)
            plt.close()
            print(f"Saved {subscale} composite boxplot to: {output_path}")
            
    except Exception as e:
        print(f"[ERROR] Failed to generate subscale boxplots: {e}")

def perform_statistical_tests(df):
    
    try:
        results = []
        scenario_pairs = [
            ("Baseline", "Cooperative"),
            ("Baseline", "Competitive"),
            ("Cooperative", "Competitive")
        ]
        
        # Test all subscales and individual items
        test_columns = [col for col in df.columns if col not in ['ParticipantID', 'Scenario', 'ScenarioLabel']]
        
        for metric in test_columns:
            for scenario1, scenario2 in scenario_pairs:
                # Get paired data
                group1 = df[df["Scenario"] == scenario1][["ParticipantID", metric]]
                group2 = df[df["Scenario"] == scenario2][["ParticipantID", metric]]
                
                # Merge to ensure we have the same participants in both groups
                paired_data = pd.merge(group1, group2, on="ParticipantID", 
                                      suffixes=('_1', '_2'))
                
                if len(paired_data) < 3:
                    # Skip if insufficient data
                    continue
                
                # Perform paired t-test
                t_stat, p_value_t = ttest_rel(paired_data[f"{metric}_1"], 
                                            paired_data[f"{metric}_2"])
                
                # Perform Wilcoxon signed-rank test (non-parametric alternative)
                w_stat, p_value_w = wilcoxon(paired_data[f"{metric}_1"], 
                                           paired_data[f"{metric}_2"])
                
                # Calculate mean difference and effect size (Cohen's d)
                mean_diff = paired_data[f"{metric}_1"].mean() - paired_data[f"{metric}_2"].mean()
                pooled_std = np.sqrt((paired_data[f"{metric}_1"].std()**2 + 
                                     paired_data[f"{metric}_2"].std()**2) / 2)
                cohen_d = mean_diff / pooled_std if pooled_std != 0 else 0
                
                results.append({
                    "Metric": metric,
                    "Scenario1": scenario1,
                    "Scenario2": scenario2,
                    "Mean1": paired_data[f"{metric}_1"].mean(),
                    "Mean2": paired_data[f"{metric}_2"].mean(),
                    "Mean_Difference": mean_diff,
                    "t_statistic": t_stat,
                    "p_value_ttest": p_value_t,
                    "w_statistic": w_stat,
                    "p_value_wilcoxon": p_value_w,
                    "Cohen's_d": cohen_d,
                    "Significant_05": p_value_t < 0.05
                })
        
        return pd.DataFrame(results)
    
    except Exception as e:
        print(f"[ERROR] Failed to perform statistical tests: {e}")
        return pd.DataFrame()

    
    except Exception as e:
        print(f"[ERROR] Failed to perform correlation analysis: {e}")
        return {}

def plot_participant_trajectories(df, output_dir="output"):
    
    try:
        os.makedirs(output_dir, exist_ok=True)
        
        # Define scenario order for x-axis
        scenario_order = ["Baseline", "Cooperative", "Competitive"]
        
        # Plot trajectories for each composite subscale
        for subscale in IMI_SUBSCALES.keys():
            plt.figure(figsize=(10, 6))
            
            # Create a pivot table for easier plotting
            pivot_df = df.pivot(index='ParticipantID', columns='Scenario', values=subscale)
            
            # Reorder columns to match our desired order
            pivot_df = pivot_df[scenario_order]
            
            # Plot each participant's trajectory
            for idx, row in pivot_df.iterrows():
                plt.plot(scenario_order, row.values, marker='o', alpha=0.6, 
                        label=f"P{int(idx)}")
            
            # Plot the mean trajectory with a thicker line
            mean_values = [pivot_df[col].mean() for col in scenario_order]
            plt.plot(scenario_order, mean_values, 'k-', linewidth=3, marker='D', 
                    markersize=10, label="Mean")
            
            plt.title(f"Individual Participant Trajectories: {subscale}")
            plt.xlabel("Scenario")
            plt.ylabel(f"{subscale} Score (1-7)")
            plt.ylim(0.5, 7.5)
            plt.grid(True, alpha=0.3)
            
            # Only show legend for smaller participant counts
            participant_count = len(pivot_df)
            if participant_count <= 10:
                plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
            
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f"{subscale}_Trajectories.png"))
            plt.close()
            
            print(f"Saved {subscale} trajectory plot")
    
    except Exception as e:
        print(f"[ERROR] Failed to plot participant trajectories: {e}")

def generate_radar_charts(df, output_dir="output"):
    
    try:
        os.makedirs(output_dir, exist_ok=True)
        
        # Get mean scores for each subscale by scenario
        subscales = list(IMI_SUBSCALES.keys())
        
        # Compute means for each scenario
        means = {}
        for scenario in ["Baseline", "Cooperative", "Competitive"]:
            means[scenario] = df[df["Scenario"] == scenario][subscales].mean().values
        
        # Number of variables
        N = len(subscales)
        
        # What will be the angle of each axis in the plot
        angles = [n / float(N) * 2 * np.pi for n in range(N)]
        angles += angles[:1]  # Close the loop
        
        # Initialise the plot
        fig = plt.figure(figsize=(10, 10))
        ax = fig.add_subplot(111, polar=True)
        
        # Add labels around the chart
        plt.xticks(angles[:-1], subscales, size=12)
        
        # Draw ylabels
        ax.set_rlabel_position(0)
        plt.yticks([1, 2, 3, 4, 5, 6, 7], ["1", "2", "3", "4", "5", "6", "7"], 
                  color="grey", size=10)
        plt.ylim(0, 7)
        
        # Plot data
        colors = {"Baseline": "green", "Cooperative": "blue", "Competitive": "red"}
        
        for scenario in ["Baseline", "Cooperative", "Competitive"]:
            values = means[scenario].tolist()
            values += values[:1]  # Close the loop
            
            ax.plot(angles, values, linewidth=2, linestyle='solid', 
                   label=scenario, color=colors[scenario])
            ax.fill(angles, values, alpha=0.1, color=colors[scenario])
        
        # Add legend
        plt.legend(loc='upper right', bbox_to_anchor=(0.1, 0.1))
        
        plt.title("IMI Subscales Across Scenarios", size=15, y=1.1)
        plt.tight_layout()
        
        output_path = os.path.join(output_dir, "IMI_Radar_Chart.png")
        plt.savefig(output_path)
        plt.close()
        
        print(f"Saved IMI radar chart to: {output_path}")
    
    except Exception as e:
        print(f"[ERROR] Failed to generate radar chart: {e}")


def run_imi_analysis(csv_path, output_dir="imi_analysis_output"):
    
    try:
        # Make sure output directory exists
        os.makedirs(output_dir, exist_ok=True)
        
        # Load data
        print("Loading IMI data...")
        df = load_imi_data(csv_path)
        if df is None:
            return
        
        # Calculate composite scores
        print("Calculating composite scores...")
        df = calculate_composite_scores(df)
        
        # Generate boxplots
        print("Generating boxplots...")
        generate_subscale_boxplots(df, output_dir)
        
        # Perform statistical tests
        print("Performing statistical tests...")
        stats_results = perform_statistical_tests(df)
        stats_results.to_csv(os.path.join(output_dir, "Statistical_Tests.csv"), index=False)
        print(f"Saved statistical test results to: {os.path.join(output_dir, 'Statistical_Tests.csv')}")
        
        # Radar charts
        print("Generating radar charts...")
        generate_radar_charts(df, output_dir)
        
        print(f"\nIMI analysis complete! All outputs saved to: {output_dir}")
        
    except Exception as e:
        print(f"[ERROR] Failed to run IMI analysis: {e}")

#endregion

#region Main Execution
if __name__ == "__main__":

    study_folder = "C:/Users/georg/OneDrive/Desktop/Personal Github/CM3203-Dissertation/User Study/Performance Data Spreadsheets"
    rpe_folder = "C:/Users/georg/OneDrive/Desktop/Personal Github/CM3203-Dissertation/User Study/Form Feedback Spreadsheets/RPE Scale/Borg RPE Scale.csv"
    rpe_output_path = "C:/Users/georg/OneDrive/Desktop/Personal Github/CM3203-Dissertation/User Study/Form Feedback Spreadsheets/RPE Scale/RPE_BoxPlot.png"
    imi_folder = "C:/Users/georg/OneDrive/Desktop/Personal Github/CM3203-Dissertation/User Study/Form Feedback Spreadsheets/Intrinsic Motivation Inventory/Intrinsic Motivation Inventory.csv"
    imi_output_path = "C:/Users/georg/OneDrive/Desktop/Personal Github/CM3203-Dissertation/User Study/Form Feedback Spreadsheets/Intrinsic Motivation Inventory/"

    analyze_rpe_differences(rpe_folder)
    generate_rpe_boxplot(rpe_folder, rpe_output_path)
    run_imi_analysis(imi_folder, imi_output_path)
    print(load_imi_data(imi_folder))
    
    for subfolder in sorted(os.listdir(study_folder)):
        subfolder_path = os.path.join(study_folder, subfolder)
        if os.path.isdir(subfolder_path):
            csv_files = [
                os.path.join(subfolder_path, f)
                for f in os.listdir(subfolder_path)
                if f.endswith(".csv")
            ]

            if len(csv_files) == 3:
                print(f"\nProcessing folder: {subfolder}")
                power_curves = {}
                heartrate_curves = {}
                all_data = {}

                for file_path in csv_files:
                    df = load_performance_data(file_path)
                    filename = os.path.basename(file_path).lower()
                    if "baseline" in filename:
                        label = "Baseline"
                    elif "cooperative" in filename:
                        label = "Cooperative"
                    elif "competitive" in filename:
                        label = "Competitive"
                    else:
                        label = os.path.splitext(os.path.basename(file_path))[0]

                    power_curves[label] = (df["ElapsedSeconds"], df["Power"])
                    heartrate_curves[label] = (df["ElapsedSeconds"], df["HeartRate"])
                    all_data[label] = df

                
                # Function calls
                plot_moving_average(all_data, "Power", False, True, subfolder_path)
                plot_moving_average(all_data, "HeartRate", True, False, subfolder_path)
                run_anova(all_data, "Power", subfolder_path)
                run_anova(all_data, "HeartRate", subfolder_path)


#endregion
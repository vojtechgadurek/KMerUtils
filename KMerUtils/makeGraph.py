import argparse
import pandas as pd
import matplotlib.pyplot as plt

print("hello")
# Set up argument parser
parser = argparse.ArgumentParser(description="Read a file and plot data.")
parser.add_argument("filename", type=str, help="The path to the input file.")
args = parser.parse_args()

# Read the file, skip comments
data = pd.read_csv(args.filename, skipinitialspace=True, encoding="utf-16")

# Check if data was loaded correctly
print("Data preview:")
print(data.head())

# Plot each column against `Prob`
plt.figure(figsize=(10, 6))
plt.plot(data["Prob"], data["Cor"], label="Cor")
plt.plot(data["Prob"], data["Miss"], label="Miss")
plt.plot(data["Prob"], data["Fail"], label="Fail")
plt.plot(data["Prob"], data["Ratio"], label="Ratio")

# Adding labels, title, and legend
plt.xlabel("Prob")
plt.ylabel("Values")
plt.title("Graph of Cor, Miss, Fail, and Ratio against Prob")
plt.legend()
plt.grid(True)
plt.show()
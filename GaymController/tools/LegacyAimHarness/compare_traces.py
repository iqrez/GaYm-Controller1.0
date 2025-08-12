import argparse
import csv
from typing import List, Tuple


def read_trace(path: str) -> List[Tuple[float, float]]:
    with open(path, newline="") as f:
        reader = csv.reader(f)
        return [tuple(map(float, row)) for row in reader]


def mean_abs_percent_error(expected: List[Tuple[float, float]], actual: List[Tuple[float, float]]) -> float:
    errors = []
    for (ex, ey), (ax, ay) in zip(expected, actual):
        denom_x = ex if ex != 0 else 1.0
        denom_y = ey if ey != 0 else 1.0
        errors.append(abs(ex - ax) / denom_x)
        errors.append(abs(ey - ay) / denom_y)
    return sum(errors) / len(errors) * 100.0


def main() -> None:
    parser = argparse.ArgumentParser(description="Compare legacy golden traces to plugin output traces.")
    parser.add_argument("golden", help="CSV file of golden legacy trace")
    parser.add_argument("replay", help="CSV file produced by plugin")
    args = parser.parse_args()

    golden = read_trace(args.golden)
    replay = read_trace(args.replay)

    error = mean_abs_percent_error(golden, replay)
    print(f"Mean absolute percentage error: {error:.3f}%")


if __name__ == "__main__":
    main()

import { describe, expect, it } from "vitest";
import { formatKhr, formatUsd, roundUsd } from "@/lib/money";

describe("money formatting", () => {
  it("shows dollars with two decimals", () => {
    expect(formatUsd(2.5)).toBe("$2.50");
    expect(formatUsd(1234.5)).toBe("$1,234.50");
  });

  it("shows riel with thousands separators and the riel sign", () => {
    expect(formatKhr(10000)).toBe("10,000៛");
    expect(formatKhr(0)).toBe("0៛");
  });

  it("rounds dollars to cents without float noise", () => {
    expect(roundUsd(0.1 + 0.2)).toBe(0.3);
    expect(roundUsd(1.005)).toBe(1.01);
  });
});

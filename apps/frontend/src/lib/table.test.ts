import { describe, expect, it } from "vitest";
import { parseTable } from "@/lib/table";

describe("parseTable", () => {
  it("accepts plain table numbers", () => {
    expect(parseTable("12")).toBe(12);
    expect(parseTable("10000")).toBe(10000);
  });

  it("uses the first value when the parameter is repeated", () => {
    expect(parseTable(["3", "9"])).toBe(3);
  });

  it.each([undefined, "", "0", "-1", "1.5", "abc", "12abc", "10001", "99999", "123456", " 5", "<script>"])(
    "ignores %j",
    (value) => {
      expect(parseTable(value)).toBeNull();
    },
  );
});

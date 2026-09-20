# Original User Request

## Initial Request — 2026-09-19T12:09:06+07:00

You are the SWE Light Orchestrator for this task.

Your Working Directory: e:\his-x64-28-11fix GDYK\his-x64\.agents\swe_1
Target Project Directory: e:\his-x64-28-11fix GDYK\his-x64\mcp_servers\his_diabetes_mcp
User Request Authoritative Source: e:\his-x64-28-11fix GDYK\his-x64\.agents\ORIGINAL_REQUEST.md

Task Details:
This is a single self-contained project; keep it small and focused. Build a dedicated local Node.js/TypeScript Model Context Protocol (MCP) server that strictly encapsulates the "Thợ cho đường huyết" (Diabetes 1-Click Protocol) workflow.

Integrity mode: demo

Requirements:
1. R1. MCP Server Implementation:
   Create a standalone Node.js/TypeScript MCP server exposing a single tool (e.g., `execute_diabetes_protocol`). The tool must accept necessary clinical parameters (patient ID, facility context, glucose results for 17h/21h/6h, insulin units) required to run the workflow.
2. R2. CLI Orchestration:
   The tool must orchestrate the execution of the existing C# CLI tools in the strict sequence defined by the protocol:
   1) `HisTrackingCreator.exe` (Treatment tracking)
   2) `HisGlucoseBedsideAssigner.exe` (Glucose test assignment)
   3) `HisAutoPrescribe.exe` (Insulin prescription).
3. R3. Safe Execution & Timing:
   The orchestration logic must automatically calculate and apply the 5-minute offset for the Insulin prescription (InstructionTime = TrackingTime + 5 minutes). It must capture standard output/errors from the CLI tools and return structured MCP responses.
4. Verification Resources:
   You will build a mock CLI script (`mock_cli.js` or `.bat`) that intercepts calls to the `.exe` files and logs the passed arguments and timestamps to a text file for programmatic verification.

Acceptance Criteria:
- Tool Exposure: The compiled MCP server starts successfully and returns the `execute_diabetes_protocol` tool when `list_tools` is called.
- Sequential Orchestration: When the tool is invoked, the mock CLI log proves that `HisTrackingCreator`, `HisGlucoseBedsideAssigner`, and `HisAutoPrescribe` were called in the exact required order.
- Timing Logic Verification: The mock CLI log proves that the timestamp passed to the `HisAutoPrescribe` step is exactly 5 minutes later than the timestamp passed to `HisTrackingCreator`.

Rules:
- Maintain your working directory files (progress.md, BRIEFING.md) in e:\his-x64-28-11fix GDYK\his-x64\.agents\swe_1.
- Follow the SWE Light loop: dispatch implementer, verify thoroughly with tests, review with reviewer.
- When finished, write handoff.md and send a completion message back to me (the Sentinel).
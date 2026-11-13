import { useEffect } from "react";

// Tables become stacked cards on phones (see the MuiTable override in App.tsx), where each
// cell is prefixed with its column header. This copies header text onto cells as data-label.
function labelTable(table: HTMLTableElement) {
  const headerRow = table.tHead?.rows[table.tHead.rows.length - 1];
  if (!headerRow) return;

  const labels: string[] = [];
  for (const th of Array.from(headerRow.cells)) {
    const text = (th.textContent ?? "").trim();
    for (let i = 0; i < Math.max(th.colSpan, 1); i++) labels.push(text);
  }

  for (const body of [...Array.from(table.tBodies), table.tFoot]) {
    if (!body) continue;
    for (const row of Array.from(body.rows)) {
      let col = 0;
      for (const cell of Array.from(row.cells)) {
        const label = labels[col] ?? "";
        if (cell.getAttribute("data-label") !== label) {
          cell.setAttribute("data-label", label);
        }
        col += Math.max(cell.colSpan, 1);
      }
    }
  }
}

function labelAllTables() {
  document
    .querySelectorAll<HTMLTableElement>("table:not([data-no-stack])")
    .forEach(labelTable);
  // Numeric keypad on phones: whole-number steps get "numeric", money/decimal steps "decimal".
  document
    .querySelectorAll<HTMLInputElement>('input[type="number"]:not([inputmode])')
    .forEach((input) => {
      const whole = !input.step || input.step === "1";
      input.setAttribute("inputmode", whole ? "numeric" : "decimal");
    });
}

export function useStackedTableLabels() {
  useEffect(() => {
    let frame = 0;
    const schedule = () => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(labelAllTables);
    };

    schedule();
    const observer = new MutationObserver(schedule);
    observer.observe(document.body, { childList: true, subtree: true });
    return () => {
      observer.disconnect();
      cancelAnimationFrame(frame);
    };
  }, []);
}

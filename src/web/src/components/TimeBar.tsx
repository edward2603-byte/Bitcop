type Props = { active: number; neutral: number };

export function TimeBar({ active, neutral }: Props) {
  return <div className="time-bar" aria-label={`Активное ${active} процентов, нейтральное ${neutral} процентов`}>
    <span className="active" style={{ width: `${active}%` }} />
    <span className="neutral" style={{ width: `${neutral}%` }} />
  </div>;
}

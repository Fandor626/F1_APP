interface DriverDotProps {
  driverCode: string
  teamColour: string
  svgX: number
  svgY: number
}

export function DriverDot({ driverCode, teamColour, svgX, svgY }: DriverDotProps) {
  return (
    <g transform={`translate(${svgX},${svgY})`} data-testid={`driver-dot-${driverCode}`}>
      <circle r={9} fill={`#${teamColour}`} stroke="#0c0e11" strokeWidth={2} />
      <text
        textAnchor="middle"
        dominantBaseline="central"
        fontSize={7}
        fontWeight={700}
        fill="#0c0e11"
      >
        {driverCode}
      </text>
    </g>
  )
}

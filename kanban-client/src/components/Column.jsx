import Card from './Card';

export default function Column({ title, cards }) {
  return (
    <div className="column">
      <h2 className="column-title">{title}</h2>
      <div className="card-stack">
        {cards.length === 0 && (
          <p className="column-empty">No cards</p>
        )}
        {cards.map((card) => (
          <Card
            key={card.id}
            title={card.title}
            description={card.description}
            assignedToUserId={card.assignedToUserId}
          />
        ))}
      </div>
    </div>
  );
}

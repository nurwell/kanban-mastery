import { Droppable } from '@hello-pangea/dnd';
import Card from './Card';

export default function Column({ id, title, cards }) {
  return (
    <div className="column">
      <h2 className="column-title">{title}</h2>
      <Droppable droppableId={id.toString()}>
        {(provided) => (
          <div
            className="card-stack"
            ref={provided.innerRef}
            {...provided.droppableProps}
          >
            {cards.length === 0 && (
              <p className="column-empty">No cards</p>
            )}
            {cards.map((card, index) => (
              <Card
                key={card.id}
                id={card.id}
                index={index}
                title={card.title}
                description={card.description}
                assignedToUserId={card.assignedToUserId}
              />
            ))}
            {provided.placeholder}
          </div>
        )}
      </Droppable>
    </div>
  );
}

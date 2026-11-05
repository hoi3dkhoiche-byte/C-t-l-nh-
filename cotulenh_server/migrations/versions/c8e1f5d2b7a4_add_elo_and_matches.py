"""add elo and matches table

Revision ID: c8e1f5d2b7a4
Revises: feedd81b9f3f
Create Date: 2026-11-05 16:20:00.000000

"""
from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision = 'c8e1f5d2b7a4'
down_revision = 'feedd81b9f3f'
branch_labels = None
depends_on = None


def upgrade():
    # Thêm các cột thống kê ELO vào users
    op.add_column('users', sa.Column('elo_rating', sa.Integer(), nullable=False, server_default='1200'))
    op.add_column('users', sa.Column('wins', sa.Integer(), nullable=False, server_default='0'))
    op.add_column('users', sa.Column('losses', sa.Integer(), nullable=False, server_default='0'))
    op.add_column('users', sa.Column('draws', sa.Integer(), nullable=False, server_default='0'))
    op.create_index(op.f('ix_users_elo_rating'), 'users', ['elo_rating'], unique=False)

    # Tạo bảng matches lưu lịch sử trận đấu
    op.create_table('matches',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('room_code', sa.String(length=8), nullable=True),
        sa.Column('red_player_id', sa.Integer(), nullable=False),
        sa.Column('blue_player_id', sa.Integer(), nullable=False),
        sa.Column('winner_id', sa.Integer(), nullable=True),
        sa.Column('result', sa.String(length=20), nullable=False),
        sa.Column('red_elo_change', sa.Integer(), nullable=False, server_default='0'),
        sa.Column('blue_elo_change', sa.Integer(), nullable=False, server_default='0'),
        sa.Column('coin_bet', sa.Integer(), nullable=False, server_default='0'),
        sa.Column('created_at', sa.DateTime(), server_default=sa.text('CURRENT_TIMESTAMP'), nullable=False),
        sa.ForeignKeyConstraint(['blue_player_id'], ['users.id'], ),
        sa.ForeignKeyConstraint(['red_player_id'], ['users.id'], ),
        sa.ForeignKeyConstraint(['winner_id'], ['users.id'], ),
        sa.PrimaryKeyConstraint('id')
    )


def downgrade():
    op.drop_table('matches')
    op.drop_index(op.f('ix_users_elo_rating'), table_name='users')
    op.drop_column('users', 'draws')
    op.drop_column('users', 'losses')
    op.drop_column('users', 'wins')
    op.drop_column('users', 'elo_rating')


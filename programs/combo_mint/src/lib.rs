// Rust Anchor Program for Combo Minting
// Deploy with: anchor deploy --provider.cluster devnet

use anchor_lang::prelude::*;
use anchor_lang::solana_program::hash::hash;

declare_id!("COMBO_MINT_PROGRAM_ID_HERE");

#[program]
pub mod combo_mint {
    use super::*;

    #[access_control(validate_combo_data(&ctx, &combo_name, damage, meter_gain, move_count))]
    pub fn create_combo(
        ctx: Context<CreateCombo>,
        combo_name: String,
        damage: u32,
        meter_gain: u32,
        move_count: u8,
        character_id: u8,
    ) -> ProgramResult {
        let combo = &mut ctx.accounts.combo_pda;
        
        combo.authority = *ctx.accounts.authority.key;
        combo.character_id = character_id;
        combo.name = combo_name;
        combo.damage = damage;
        combo.meter_gain = meter_gain;
        combo.move_count = move_count;
        combo.timestamp = Clock::get()?.unix_timestamp;
        combo.bump = ctx.bumps.combo_pda;

        let combo_seed = compute_combo_seed(
            combo.name.as_bytes(),
            damage,
            meter_gain,
            move_count,
            character_id,
        );
        combo.combo_hash = combo_seed;

        emit!(ComboCreated {
            combo: ctx.accounts.combo_pda.key(),
            authority: combo.authority,
            character_id,
            damage,
            timestamp: combo.timestamp,
        });

        Ok(())
    }

    pub fn verify_combo(ctx: Context<VerifyCombo>, moves: Vec<u8>) -> ProgramResult {
        let combo = &mut ctx.accounts.combo_pda;
        combo.verification_count += 1;
        combo.last_verified = Clock::get()?.unix_timestamp;

        emit!(ComboVerified {
            combo: ctx.accounts.combo_pda.key(),
            moves_count: moves.len() as u8,
            timestamp: combo.last_verified,
        });

        Ok(())
    }

    #[access_control(only_authority(&ctx))]
    pub fn close_combo(ctx: Context<CloseCombo>) -> ProgramResult {
        let destination = &ctx.accounts.destination;
        let combo_pda = &mut ctx.accounts.combo_pda;

        **destination.try_borrow_mut_lamports()? += **combo_pda.try_borrow_lamports()?;
        **combo_pda.try_borrow_mut_lamports()? = 0;

        Ok(())
    }
}

fn compute_combo_seed(
    name: &[u8],
    damage: u32,
    meter_gain: u32,
    move_count: u8,
    character_id: u8,
) -> [u8; 32] {
    let mut input = Vec::with_capacity(64);
    input.extend_from_slice(name);
    input.extend_from_slice(&damage.to_le_bytes());
    input.extend_from_slice(&meter_gain.to_le_bytes());
    input.push(move_count);
    input.push(character_id);
    
    let hash_result = hash(&input);
    hash_result.to_bytes()
}

fn validate_combo_data(
    _ctx: &Context<CreateCombo>,
    combo_name: &String,
    damage: &u32,
    meter_gain: &u32,
    move_count: &u8,
) -> Result<()> {
    require!(combo_name.len() <= 64, ComboError::NameTooLong);
    require!(*damage > 0 && *damage <= 1000, ComboError::InvalidDamage);
    require!(*meter_gain > 0 && *meter_gain <= 100, ComboError::InvalidMeterGain);
    require!(*move_count > 0 && *move_count <= 20, ComboError::InvalidMoveCount);
    Ok(())
}

fn verify_move_sequence(_ctx: &Context<VerifyCombo>, moves: &Vec<u8>) -> Result<()> {
    require!(moves.len() <= 20, ComboError::TooManyMoves);
    Ok(())
}

fn only_authority(ctx: &Context<CloseCombo>) -> Result<()> {
    require!(
        ctx.accounts.combo_pda.authority == *ctx.accounts.authority.key,
        ComboError::Unauthorized
    );
    Ok(())
}

#[derive(Accounts)]
pub struct CreateCombo<'info> {
    #[account(signer)]
    pub authority: AccountInfo<'info>,
    #[account(
        init,
        seeds = [b"combo", authority.key.as_ref()],
        bump,
        space = 256,
        payer = authority,
    )]
    pub combo_pda: Account<'info, ComboAccount>,
    pub system_program: Program<'info, System>,
    #[account(address = sysvar::rent::ID)]
    pub rent: Sysvar<'info, Rent>,
}

#[derive(Accounts)]
pub struct VerifyCombo<'info> {
    #[account(mut)]
    pub combo_pda: Account<'info, ComboAccount>,
    /// CHECK
    #[account(signer)]
    pub verifier: UncheckedAccount<'info>,
}

#[derive(Accounts)]
pub struct CloseCombo<'info> {
    #[account(mut)]
    pub combo_pda: Account<'info, ComboAccount>,
    #[account(signer)]
    pub authority: AccountInfo<'info>,
    /// CHECK
    pub destination: UncheckedAccount<'info>,
}

#[account]
pub struct ComboAccount {
    pub authority: Pubkey,
    pub character_id: u8,
    pub name: String,
    pub damage: u32,
    pub meter_gain: u32,
    pub move_count: u8,
    pub timestamp: i64,
    pub combo_hash: [u8; 32],
    pub verification_count: u32,
    pub last_verified: i64,
    pub bump: u8,
}

#[event]
pub struct ComboCreated {
    pub combo: Pubkey,
    pub authority: Pubkey,
    pub character_id: u8,
    pub damage: u32,
    pub timestamp: i64,
}

#[event]
pub struct ComboVerified {
    pub combo: Pubkey,
    pub moves_count: u8,
    pub timestamp: i64,
}

#[error]
pub enum ComboError {
    #[msg("Combo name too long")]
    NameTooLong,
    #[msg("Invalid damage value")]
    InvalidDamage,
    #[msg("Invalid meter gain value")]
    InvalidMeterGain,
    #[msg("Invalid move count")]
    InvalidMoveCount,
    #[msg("Too many moves")]
    TooManyMoves,
    #[msg("Unauthorized")]
    Unauthorized,
}

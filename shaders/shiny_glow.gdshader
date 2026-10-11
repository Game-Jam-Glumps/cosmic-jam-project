shader_type canvas_item;

uniform vec4 glow_color : source_color = vec4(0.3, 0.9, 1.0, 1.0);
uniform float glow_size = 2.5;
uniform float pulse_speed = 2.0;

void fragment()
{
    vec4 sprite_color = texture(TEXTURE, UV);
    vec2 pixel = TEXTURE_PIXEL_SIZE * glow_size;

    float surrounding_alpha = 0.0;

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(pixel.x, 0.0)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(-pixel.x, 0.0)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(0.0, pixel.y)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(0.0, -pixel.y)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(pixel.x, pixel.y)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(-pixel.x, pixel.y)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(pixel.x, -pixel.y)).a
    );

    surrounding_alpha = max(
        surrounding_alpha,
        texture(TEXTURE, UV + vec2(-pixel.x, -pixel.y)).a
    );

    float outline_alpha = max(surrounding_alpha - sprite_color.a, 0.0);
    float pulse = 0.75 + 0.25 * sin(TIME * pulse_speed);

    vec4 glow = vec4(
        glow_color.rgb,
        outline_alpha * glow_color.a * pulse
    );

    COLOR = sprite_color + glow;
}
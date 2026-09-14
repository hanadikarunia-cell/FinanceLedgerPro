import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Card, Text } from 'react-native-paper';

interface StatCardProps {
  label: string;
  value: string;
  accentColor?: string;
  caption?: string;
}

/** A compact KPI card used across the Dashboard. */
export function StatCard({ label, value, accentColor, caption }: StatCardProps) {
  return (
    <Card style={styles.card} mode="elevated">
      <Card.Content>
        <Text variant="labelMedium" style={styles.label}>
          {label}
        </Text>
        <Text
          variant="headlineSmall"
          style={[styles.value, accentColor ? { color: accentColor } : null]}
        >
          {value}
        </Text>
        {caption ? (
          <Text variant="bodySmall" style={styles.caption}>
            {caption}
          </Text>
        ) : null}
      </Card.Content>
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    flex: 1,
    margin: 6,
    minWidth: 140,
  },
  label: {
    opacity: 0.7,
    marginBottom: 4,
  },
  value: {
    fontWeight: '700',
  },
  caption: {
    opacity: 0.6,
    marginTop: 2,
  },
});

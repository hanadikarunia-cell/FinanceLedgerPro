import React, { useMemo, useState } from 'react';
import { Platform, ScrollView, StyleSheet, View } from 'react-native';
import {
  Button,
  Menu,
  SegmentedButtons,
  Snackbar,
  Text,
  TextInput,
} from 'react-native-paper';
import DateTimePicker from '@react-native-community/datetimepicker';
import { FormInput } from './FormInput';
import { useBranches } from '@/hooks/useBranches';
import { useCreateTransaction } from '@/hooks/useCreateTransaction';
import type {
  ApprovalStatus,
  Branch,
  CreateTransactionRequest,
  TransactionType,
} from '@/types';
import { palette } from '@/theme';

interface TransactionFormProps {
  type: TransactionType;
  /** Category suggestions differ for income vs expense. */
  categories: string[];
}

interface FormErrors {
  category?: string;
  amount?: string;
  branch?: string;
}

function toIsoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

/**
 * Shared Income/Expense entry form. Saves online, or transparently queues to
 * the offline outbox and surfaces that to the user.
 */
export function TransactionForm({ type, categories }: TransactionFormProps) {
  const { data: branches } = useBranches();
  const createTx = useCreateTransaction();

  const [category, setCategory] = useState('');
  const [categoryMenu, setCategoryMenu] = useState(false);
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState(new Date());
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [branch, setBranch] = useState('');
  const [branchMenu, setBranchMenu] = useState(false);
  const [approvalStatus, setApprovalStatus] = useState<ApprovalStatus>('Draft');
  const [errors, setErrors] = useState<FormErrors>({});
  const [snack, setSnack] = useState<string | null>(null);

  const accent = type === 'Income' ? palette.income : palette.expense;

  const validate = (): boolean => {
    const next: FormErrors = {};
    if (!category.trim()) next.category = 'Category is required';
    const parsed = Number(amount);
    if (!amount.trim() || Number.isNaN(parsed) || parsed <= 0) {
      next.amount = 'Enter a valid amount greater than 0';
    }
    if (!branch.trim()) next.branch = 'Branch is required';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const reset = () => {
    setCategory('');
    setDescription('');
    setAmount('');
    setDate(new Date());
    setBranch('');
    setApprovalStatus('Draft');
    setErrors({});
  };

  const onSubmit = async () => {
    if (!validate()) return;
    const payload: CreateTransactionRequest = {
      type,
      category: category.trim(),
      description: description.trim(),
      amount: Number(amount),
      date: toIsoDate(date),
      branch,
      approvalStatus,
    };
    try {
      const result = await createTx.mutateAsync(payload);
      setSnack(
        result.queuedOffline
          ? 'Saved offline — will sync when you are back online.'
          : `${type} saved successfully.`,
      );
      reset();
    } catch {
      setSnack('Could not save. Please try again.');
    }
  };

  const branchName = useMemo(
    () => branches?.find((b: Branch) => b.id === branch)?.name ?? 'Select branch',
    [branches, branch],
  );

  return (
    <>
      <ScrollView
        contentContainerStyle={styles.container}
        keyboardShouldPersistTaps="handled"
      >
        <Text variant="titleLarge" style={[styles.heading, { color: accent }]}>
          New {type}
        </Text>

        {/* Category — free text with quick-pick suggestions */}
        <View>
          <Menu
            visible={categoryMenu}
            onDismiss={() => setCategoryMenu(false)}
            anchor={
              <FormInput
                label="Category"
                value={category}
                error={errors.category}
                onChangeText={setCategory}
                right={
                  <TextInput.Icon
                    icon="menu-down"
                    onPress={() => setCategoryMenu(true)}
                  />
                }
              />
            }
          >
            {categories.map((c) => (
              <Menu.Item
                key={c}
                title={c}
                onPress={() => {
                  setCategory(c);
                  setCategoryMenu(false);
                }}
              />
            ))}
          </Menu>
        </View>

        <FormInput
          label="Description"
          value={description}
          onChangeText={setDescription}
          multiline
        />

        <FormInput
          label="Amount"
          value={amount}
          error={errors.amount}
          onChangeText={setAmount}
          keyboardType="decimal-pad"
          left={<TextInput.Affix text="$" />}
        />

        {/* Date */}
        <Button
          mode="outlined"
          icon="calendar"
          style={styles.dateButton}
          onPress={() => setShowDatePicker(true)}
        >
          {toIsoDate(date)}
        </Button>
        {showDatePicker ? (
          <DateTimePicker
            value={date}
            mode="date"
            display={Platform.OS === 'ios' ? 'inline' : 'default'}
            onChange={(_event, selected) => {
              setShowDatePicker(false);
              if (selected) setDate(selected);
            }}
          />
        ) : null}

        {/* Branch picker */}
        <View style={styles.field}>
          <Menu
            visible={branchMenu}
            onDismiss={() => setBranchMenu(false)}
            anchor={
              <Button
                mode="outlined"
                icon="office-building"
                onPress={() => setBranchMenu(true)}
                style={errors.branch ? styles.branchError : undefined}
              >
                {branchName}
              </Button>
            }
          >
            {(branches ?? []).map((b: Branch) => (
              <Menu.Item
                key={b.id}
                title={b.name}
                onPress={() => {
                  setBranch(b.id);
                  setBranchMenu(false);
                }}
              />
            ))}
          </Menu>
          {errors.branch ? (
            <Text style={styles.errorText}>{errors.branch}</Text>
          ) : null}
        </View>

        {/* Approval status */}
        <Text variant="labelLarge" style={styles.approvalLabel}>
          Approval
        </Text>
        <SegmentedButtons
          value={approvalStatus}
          onValueChange={(v) => setApprovalStatus(v as ApprovalStatus)}
          buttons={[
            { value: 'Draft', label: 'Draft' },
            { value: 'Submitted', label: 'Submit' },
          ]}
        />

        <Button
          mode="contained"
          style={[styles.submit, { backgroundColor: accent }]}
          loading={createTx.isPending}
          disabled={createTx.isPending}
          onPress={onSubmit}
        >
          Save {type}
        </Button>
      </ScrollView>

      <Snackbar
        visible={Boolean(snack)}
        onDismiss={() => setSnack(null)}
        duration={3500}
      >
        {snack}
      </Snackbar>
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 16,
    gap: 8,
    paddingBottom: 48,
  },
  heading: {
    marginBottom: 8,
    fontWeight: '700',
  },
  dateButton: {
    marginTop: 4,
  },
  field: {
    marginTop: 8,
  },
  branchError: {
    borderColor: palette.expense,
  },
  errorText: {
    color: palette.expense,
    fontSize: 12,
    marginTop: 4,
    marginLeft: 4,
  },
  approvalLabel: {
    marginTop: 12,
    marginBottom: 4,
  },
  submit: {
    marginTop: 24,
  },
});
